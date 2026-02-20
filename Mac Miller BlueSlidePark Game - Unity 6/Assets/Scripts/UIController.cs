using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Main menu controller matching original StartMenu + TextEntry classes from SWF.
/// Decompiled source: StartMenu.as, TextEntry.as
///
/// Original TextEntry behavior:
///   - Manual key-by-key input: A-Z keys + Space only (all uppercase)
///   - Backspace deletes last character
///   - Enter/Return submits name → sends "NameEntered" message to startScreen
///   - Saves name to PlayerPrefs "playerName"
///   - Cursor blink via InvokeRepeating("CursorOn", 0, blinkRate) and
///     InvokeRepeating("CursorOff", blinkRate*0.5, blinkRate)
///   - Display: text + (cursorOn ? "_" : "") on UILabel
///
/// Original StartMenu behavior:
///   - Fetches Facebook name via ExternalCall.FetchName()
///   - After name entry: hides nameentryUI, Invoke("DelayedHaveName", 0.3)
///   - Start game: Destroy(self), Play startSound, Instantiate Resources.Load("SlideController")
///   - Button hover: play buttonHoverSound
/// </summary>
public class UIController : MonoBehaviour
{
    [SerializeField] private GameObject txtBox;
    [SerializeField] private GameObject box;
    [SerializeField] private GameObject spaceTxt;

    [Header("Text Entry (original TextEntry class)")]
    [SerializeField] private int textMaxLength = 20;
    [SerializeField] private float cursorBlinkRate = 0.5f;

    private bool initialized;
    private bool nameEntered;
    private bool hasStartedTyping;
    private string playerName = "";
    private string placeholderText;
    private Text nameTextComponent;

    // Cursor blinking (original TextEntry had cursorOn + blinkRate)
    private float cursorBlinkTimer;
    private bool cursorVisible = true;

    private void Start()
    {
        box = GameObject.Find("MainBox");
        spaceTxt = GameObject.Find("pressSpaceTxt");
        txtBox = GameObject.Find("nameText");
        if (txtBox != null)
            nameTextComponent = txtBox.GetComponent<Text>();
        if (nameTextComponent != null)
            placeholderText = nameTextComponent.text;
        if (spaceTxt != null)
            spaceTxt.SetActive(false);
        initialized = true;
    }

    private void UpdateNameDisplay()
    {
        if (nameTextComponent == null) return;

        if (!hasStartedTyping)
        {
            nameTextComponent.text = placeholderText;
        }
        else
        {
            // Blinking cursor like original TextEntry
            string cursor = cursorVisible ? "_" : " ";
            nameTextComponent.text = playerName + cursor;
        }
    }

    public void LoadSceneButton()
    {
        if (!nameEntered && playerName.Length > 0)
        {
            NameEntered();
        }
    }

    /// <summary>
    /// Matches original StartMenu_NameEntered -> DelayedHaveName flow.
    /// </summary>
    private void NameEntered()
    {
        nameEntered = true;

        // Save player name (original used playerName field passed to leaderboard)
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();

        if (box != null) box.SetActive(false);
        if (spaceTxt != null) spaceTxt.SetActive(true);

        // Play start sound
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayStart();
    }

    private void Update()
    {
        if (!initialized)
            return;

        // Blink cursor
        cursorBlinkTimer += Time.deltaTime;
        if (cursorBlinkTimer >= cursorBlinkRate)
        {
            cursorBlinkTimer = 0f;
            cursorVisible = !cursorVisible;
        }

        if (!nameEntered)
        {
            // Original TextEntry_Update: manual key-by-key A-Z + Space only (uppercase)
            for (KeyCode k = KeyCode.A; k <= KeyCode.Z; k++)
            {
                if (Input.GetKeyDown(k))
                {
                    if (playerName.Length < textMaxLength)
                    {
                        if (!hasStartedTyping) hasStartedTyping = true;
                        playerName += (char)('A' + (k - KeyCode.A));
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.Space) && playerName.Length < textMaxLength)
            {
                if (!hasStartedTyping) hasStartedTyping = true;
                playerName += " ";
            }
            if (playerName.Length > textMaxLength)
                playerName = playerName.Substring(0, textMaxLength);
            if (Input.GetKeyDown(KeyCode.Backspace) && playerName.Length > 0)
            {
                playerName = playerName.Substring(0, playerName.Length - 1);
                if (playerName.Length == 0)
                    hasStartedTyping = false;
            }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (playerName.Length > 0)
                    NameEntered();
            }
            UpdateNameDisplay();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }
    }

    public void DisplayNewUI(string option)
    {
        switch (option)
        {
            case "music player":
                Debug.Log("Show UI for music player...");
                break;
            case "options":
                Debug.Log("Show UI for options...");
                break;
            case "quit":
                Debug.Log("Are you sure?...");
                break;
        }
    }
}
