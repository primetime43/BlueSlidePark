using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Main menu controller matching original StartMenu + TextEntry classes from SWF.
///
/// Original StartMenu fields: restarted, startButton, startButtonHover, style,
/// buttonRect, haveName, nameentryUI, wasHovering, sendName, playerName,
/// startSound, buttonHoverSound.
///
/// Original TextEntry fields: text, enterText, textMaxLength (20), message,
/// startScreen, cursorOn, blinkRate.
///
/// Original flow: TextEntry for name input -> StartMenu_NameEntered ->
/// StartMenu_DelayedHaveName -> load level.
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
            // Handle typing (original TextEntry_Update)
            foreach (char c in Input.inputString)
            {
                if (c == '\b') // backspace
                {
                    if (playerName.Length > 0)
                        playerName = playerName.Substring(0, playerName.Length - 1);
                    if (playerName.Length == 0)
                        hasStartedTyping = false;
                }
                else if (c == '\n' || c == '\r') // enter
                {
                    if (playerName.Length > 0)
                        NameEntered();
                }
                else if (playerName.Length < textMaxLength)
                {
                    if (!hasStartedTyping)
                        hasStartedTyping = true;
                    playerName += c;
                }
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
