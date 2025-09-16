using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonFill : MonoBehaviour
{
    [Header("Button Fill Settings")]
    public Image Fill; // Reference to the fill image
    public TextMeshProUGUI PercentageText; // Reference to the percentage text
    public float percentage = 0; // Percentage value to fill the button

    void Update()
    {
        // Update the fill amount based on the percentage
        Fill.fillAmount = Mathf.Lerp(0f, 1f, percentage / 100f);
        // Update the percentage text
        PercentageText.text = $"{percentage:0}%";
    }

    // This method will be called to set the fill percentage dynamically
    public void SetFillPercentage(float newPercentage)
    {
        percentage = Mathf.Clamp(newPercentage, 0, 100);
    }
}
