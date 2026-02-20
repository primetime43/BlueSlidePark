using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Number display matching original Numbers class from SWF.
/// Original: 10 digit textures (0-9.png), instantiated a plane per digit.
///
/// This version supports both Text-based display and Image-based display
/// using the original digit textures from Resources/Digits/.
///
/// Original fields: displayedNumber.
/// Original methods: Numbers_Start, Numbers_Update, Numbers_DisplayNumber.
/// Animates score number changes with a counting-up effect.
/// </summary>
public class NumbersDisplay : MonoBehaviour
{
    [SerializeField] private Text numberText;
    [SerializeField] private float countSpeed = 10f;

    [Header("Original Digit Sprites (auto-loaded from Resources/Digits/)")]
    [SerializeField] private Image[] digitImages;
    private Sprite[] digitSprites;

    private int targetNumber;
    private float displayedNumber;

    private void Start()
    {
        if (numberText == null)
            numberText = GetComponent<Text>();

        LoadDigitSprites();
    }

    private void LoadDigitSprites()
    {
        digitSprites = new Sprite[10];
        for (int i = 0; i < 10; i++)
        {
            Texture2D tex = Resources.Load<Texture2D>("Digits/" + i);
            if (tex != null)
            {
                digitSprites[i] = Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f));
            }
        }
    }

    private void Update()
    {
        if (Mathf.Abs(displayedNumber - targetNumber) > 0.5f)
        {
            displayedNumber = Mathf.MoveTowards(displayedNumber, targetNumber, countSpeed * Time.deltaTime);
            UpdateDisplay();
        }
    }

    public void DisplayNumber(int number)
    {
        targetNumber = number;
    }

    public void SetImmediate(int number)
    {
        targetNumber = number;
        displayedNumber = number;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        int num = Mathf.FloorToInt(displayedNumber);

        // Text-based display (always update)
        if (numberText != null)
            numberText.text = num.ToString();

        // Image-based display using original digit sprites
        if (digitImages != null && digitImages.Length > 0 && digitSprites != null)
        {
            string numStr = num.ToString();
            // Pad with leading zeros to fill available digit images
            while (numStr.Length < digitImages.Length)
                numStr = "0" + numStr;

            // Only use last N digits if number is larger than available images
            if (numStr.Length > digitImages.Length)
                numStr = numStr.Substring(numStr.Length - digitImages.Length);

            for (int i = 0; i < digitImages.Length; i++)
            {
                if (digitImages[i] != null)
                {
                    int digit = numStr[i] - '0';
                    if (digit >= 0 && digit <= 9 && digitSprites[digit] != null)
                    {
                        digitImages[i].sprite = digitSprites[digit];
                        digitImages[i].enabled = true;
                    }
                }
            }
        }
    }
}
