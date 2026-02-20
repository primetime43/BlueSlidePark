using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Leaderboard client matching the original game's HTTPPoke/Leaderboard/ExternalCall classes.
///
/// Original endpoints (now defunct):
///   POST http://macmillerofficial.umgfacebook.com/post_scores.php
///   GET  http://macmillerofficial.umgfacebook.com/request.php?score={s}&name={n}&hash={h}
///
/// Hash formula: SHA1(score + name + "a1e902e")
/// Response format: TSV with rank:, name:, score: fields
///
/// This implementation can be pointed at a custom server by changing serverBaseUrl.
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    [Header("Server Configuration")]
    [SerializeField] private string serverBaseUrl = "";
    [SerializeField] private string fetchEndpoint = "/request.php";
    [SerializeField] private string submitEndpoint = "/post_scores.php";

    [Header("Anti-Cheat (original values from SWF)")]
    [SerializeField] private string leaderboardSalt = "a1e902e";
    [SerializeField] private string postScoreSalt = "6OEIr6ZkYm";

    private List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

    public List<LeaderboardEntry> Entries => entries;

    public event Action OnLeaderboardUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Fetch leaderboard from server (mirrors HTTPPoke.FetchLeaderboardAS)
    /// </summary>
    public void FetchLeaderboard(int score, string playerName)
    {
        if (string.IsNullOrEmpty(serverBaseUrl))
        {
            Debug.Log("[LeaderboardManager] No server URL configured. Skipping fetch.");
            return;
        }

        StartCoroutine(FetchLeaderboardCoroutine(score, playerName));
    }

    private IEnumerator FetchLeaderboardCoroutine(int score, string playerName)
    {
        string hash = ComputeHash(score.ToString() + playerName + leaderboardSalt);
        string url = serverBaseUrl + fetchEndpoint
            + "?score=" + score
            + "&name=" + UnityWebRequest.EscapeURL(playerName)
            + "&hash=" + hash;

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                ProcessLeaderboard(req.downloadHandler.text);
                OnLeaderboardUpdated?.Invoke();
            }
            else
            {
                Debug.LogWarning("[LeaderboardManager] Fetch failed: " + req.error);
            }
        }
    }

    /// <summary>
    /// Submit score to server (mirrors ExternalCall.PostScore)
    /// </summary>
    public void SubmitScore(int score, string userId)
    {
        if (string.IsNullOrEmpty(serverBaseUrl))
        {
            Debug.Log("[LeaderboardManager] No server URL configured. Skipping submit.");
            return;
        }

        StartCoroutine(SubmitScoreCoroutine(score, userId));
    }

    private IEnumerator SubmitScoreCoroutine(int score, string userId)
    {
        WWWForm form = new WWWForm();
        form.AddField("user_id", userId);
        form.AddField("score", score);

        string url = serverBaseUrl + submitEndpoint;

        using (UnityWebRequest req = UnityWebRequest.Post(url, form))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogWarning("[LeaderboardManager] Submit failed: " + req.error);
        }
    }

    /// <summary>
    /// Parse TSV leaderboard response (mirrors Leaderboard.ProcessLeaderboard).
    /// Original format: rank:\tname:\tscore:\n per entry.
    /// </summary>
    private void ProcessLeaderboard(string response)
    {
        entries.Clear();

        string[] lines = response.Split('\n');
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] parts = line.Split('\t');
            if (parts.Length >= 3)
            {
                var entry = new LeaderboardEntry
                {
                    rank = int.TryParse(parts[0].Replace("rank:", "").Trim(), out int r) ? r : 0,
                    name = parts[1].Replace("name:", "").Trim(),
                    score = int.TryParse(parts[2].Replace("score:", "").Trim(), out int s) ? s : 0
                };
                entries.Add(entry);
            }
        }
    }

    /// <summary>
    /// SHA1 hash computation matching the original SHA1.as implementation.
    /// </summary>
    private string ComputeHash(string input)
    {
        using (SHA1 sha1 = SHA1.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = sha1.ComputeHash(bytes);
            StringBuilder sb = new StringBuilder(40);
            foreach (byte b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}

[Serializable]
public class LeaderboardEntry
{
    public int rank;
    public string name;
    public int score;
}
