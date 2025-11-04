using UnityEngine;
using UnityEngine.UI;

public class ExitButton : MonoBehaviour
{
    [SerializeField] private Button exitButton;
    
    void Start()
    {
        // Only show exit button in WebGL builds
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            if (exitButton != null)
            {
                exitButton.gameObject.SetActive(true);
                exitButton.onClick.AddListener(OnExitButtonClicked);
            }
        }
        else
        {
            // Hide in editor/standalone
            if (exitButton != null)
            {
                exitButton.gameObject.SetActive(false);
            }
        }
    }
    
    private void OnExitButtonClicked()
    {
        Debug.Log("🚪 Exit button clicked");
        
        if (StorageBridge.Instance != null)
        {
            StorageBridge.Instance.ExitGame();
        }
        else
        {
            Debug.LogError("❌ StorageBridge instance not found!");
        }
    }
    
    void OnDestroy()
    {
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnExitButtonClicked);
        }
    }
}