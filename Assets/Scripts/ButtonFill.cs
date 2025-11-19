using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonFill : MonoBehaviour
{
    [Header("Button Fill Settings")]
    public Image Fill; // Reference to the fill image
    public TextMeshProUGUI PercentageText; // Reference to the percentage text
    public float percentage = 0; // Percentage value to fill the button

    private bool isInitialized = false;

    void Start()
    {
        // Validate references
        if (Fill == null)
        {
            Debug.LogError($"❌ ButtonFill on {gameObject.name}: Fill Image is not assigned!");
        }
        if (PercentageText == null)
        {
            Debug.LogError($"❌ ButtonFill on {gameObject.name}: PercentageText is not assigned!");
        }

        isInitialized = (Fill != null && PercentageText != null);
        
        if (isInitialized)
        {
            Debug.Log($"✅ ButtonFill on {gameObject.name} initialized successfully");
        }
    }

    void Update()
    {
        // ✅ CRITICAL FIX: Only update if components exist
        if (!isInitialized)
        {
            return;
        }

        try
        {
            // Update the fill amount based on the percentage
            Fill.fillAmount = Mathf.Lerp(0f, 1f, percentage / 100f);
            
            // Update the percentage text
            PercentageText.text = $"{percentage:0}%";
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Error in ButtonFill.Update on {gameObject.name}: {ex.Message}");
            isInitialized = false; // Stop trying to update
        }
    }

    // This method will be called to set the fill percentage dynamically
    public void SetFillPercentage(float newPercentage)
    {
        try
        {
            percentage = Mathf.Clamp(newPercentage, 0, 100);
            
            // Immediately update visual if initialized
            if (isInitialized && Fill != null)
            {
                Fill.fillAmount = Mathf.Lerp(0f, 1f, percentage / 100f);
            }
            if (isInitialized && PercentageText != null)
            {
                PercentageText.text = $"{percentage:0}%";
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Error in SetFillPercentage on {gameObject.name}: {ex.Message}");
        }
    }
}