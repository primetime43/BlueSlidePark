using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    [SerializeField] private GameObject txtBox;
    [SerializeField] private GameObject box;
    [SerializeField] private GameObject spaceTxt;

    private bool initialized;
    private bool nameEntered;
    private bool hasStartedTyping;
    private string playerName = "";
    private string placeholderText;
    private Text nameTextComponent;

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
            nameTextComponent.text = placeholderText;
        else
            nameTextComponent.text = playerName + "_";
    }

    public void LoadSceneButton()
    {
        if (!nameEntered && playerName.Length > 0)
        {
            nameEntered = true;
            if (box != null) box.SetActive(false);
            if (spaceTxt != null) spaceTxt.SetActive(true);
        }
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (!nameEntered)
        {
            // Handle typing
            foreach (char c in Input.inputString)
            {
                if (c == '\b') // backspace
                {
                    if (playerName.Length > 0)
                        playerName = playerName.Substring(0, playerName.Length - 1);
                }
                else if (c == '\n' || c == '\r') // enter
                {
                    if (playerName.Length > 0)
                    {
                        nameEntered = true;
                        if (box != null) box.SetActive(false);
                        if (spaceTxt != null) spaceTxt.SetActive(true);
                    }
                }
                else if (playerName.Length < 20) // max name length
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
