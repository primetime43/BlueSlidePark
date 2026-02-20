using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Number display matching original Numbers class from SWF.
/// Original fields: displayedNumber.
/// Original methods: Numbers_Start, Numbers_Update, Numbers_DisplayNumber.
/// Animates score number changes with a counting-up effect.
/// Used for both "Best Numbers" display and main score.
/// </summary>
public class NumbersDisplay : MonoBehaviour
{
    [SerializeField] private Text numberText;
    [SerializeField] private float countSpeed = 10f;

    private int targetNumber;
    private float displayedNumber;

    private void Start()
    {
        if (numberText == null)
            numberText = GetComponent<Text>();
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
        if (numberText != null)
            numberText.text = Mathf.FloorToInt(displayedNumber).ToString();
    }
}
