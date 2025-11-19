using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.EventSystems;

public class FoodButton : MonoBehaviour
{
    public PetBehaviour petBehaviour;
    public GameObject NotEnoughFoodPanel;
    public GameObject FoodIcon;
    public TMP_Text[] Foodtext;
    private bool isClickable = true;
    private int foodquantity;
    private string apiUrl;
    private string apiUrlSecondary;
    private int petId;
    public HorizontalPageScroller HPS;

    [Header("Touch Settings")]
    public float touchExpandArea = 0.5f;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool forceWebGLMode = false;

    private void Start()
    {
        try
        {
            Debug.Log("🍔 FoodButton.Start() - Beginning");
            
            // Initialize API URL and pet ID from PlayerPrefs
            string petKey = PlayerPrefKeys.PetPrefix;
            apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL);
            apiUrlSecondary = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary);
            petId = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);
            
            LogPlatformInfo();

            // Ensure collider exists for WebGL touch
            EnsureCollider();
            
            Debug.Log("✅ FoodButton.Start() - Completed");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Error in FoodButton.Start(): {ex.Message}");
            Debug.LogError($"Stack: {ex.StackTrace}");
        }
    }

    private void LogPlatformInfo()
    {
        string platform = "Unknown";
        
#if UNITY_EDITOR
        platform = "Unity Editor";
#elif UNITY_WEBGL
        platform = "WebGL Build";
#elif UNITY_ANDROID
        platform = "Android";
#elif UNITY_IOS
        platform = "iOS";
#elif UNITY_STANDALONE
        platform = "Standalone PC";
#endif

        Debug.Log($"=== FoodButton Platform Info ===");
        Debug.Log($"Platform: {platform}");
        Debug.Log($"Is Mobile: {Application.isMobilePlatform}");
        Debug.Log($"Force WebGL Mode: {forceWebGLMode}");
        Debug.Log($"Touch Supported: {Input.touchSupported}");
        Debug.Log($"================================");
    }

    private void EnsureCollider()
    {
        try
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col == null)
            {
                Debug.Log("⚠️ No collider found, adding BoxCollider2D");
                
                BoxCollider2D boxCol = gameObject.AddComponent<BoxCollider2D>();
                
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Vector2 baseSize = new Vector2(sr.bounds.size.x, sr.bounds.size.y);
                    Vector2 expandedSize = baseSize + new Vector2(touchExpandArea, touchExpandArea);
                    boxCol.size = expandedSize;
                    
                    Debug.Log($"✅ BoxCollider2D added with size: {expandedSize}");
                }
                else
                {
                    Debug.LogWarning("⚠️ No SpriteRenderer found, using default collider size");
                }
            }
            else
            {
                Debug.Log("✅ Collider already exists");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Error in EnsureCollider(): {ex.Message}");
            Debug.LogError($"Stack: {ex.StackTrace}");
        }
    }

    // ✅ ONLY use OnMouseDown - works in WebGL without Input System issues
    private void OnMouseDown()
    {
        if (!isClickable) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Debug.Log("🍔 Food button clicked!");
        HandleFoodClick();
    }

    // ✅ REMOVED Update() method entirely to avoid Input System conflicts
    // OnMouseDown() handles all click/touch detection automatically

    private void HandleFoodClick()
    {
        try
        {
            foodquantity = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetFoodStack);

            if (foodquantity == 0)
            {
                if (NotEnoughFoodPanel != null)
                    NotEnoughFoodPanel.SetActive(true);
                return;
            }

            if (petBehaviour != null)
                petBehaviour.OnFeedButtonPressed();
                
            if (FoodIcon != null)
                FoodIcon.SetActive(false);

            StartCoroutine(DisableClickForDuration(10f));
            StartCoroutine(DecreaseFoodOnBackend());
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Error in HandleFoodClick: {ex.Message}");
        }
    }

    private IEnumerator DisableClickForDuration(float duration)
    {
        isClickable = false;

        yield return new WaitForSeconds(duration);

        isClickable = true;
        
        if (FoodIcon != null)
            FoodIcon.SetActive(true);
            
        foodquantity -= 1;
        
        int playfulness = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetPlayfulness);
        int hunger = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHunger) + 30;
        int bath = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHygiene) - 2;
        
        if (hunger >= 100)
        {
            hunger = 100;
        }
        if (bath <= 0)
        {
            bath = 0;
        }
        
        int sleep = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetSleep);
        
        PlayerPrefs.SetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHygiene, bath);
        PlayerPrefs.SetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHunger, hunger);
        PlayerPrefs.SetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetFoodStack, foodquantity);
        PlayerPrefs.Save();
        
        int[] stats = new int[] { playfulness, hunger, bath, sleep };
        
        if (HPS != null)
            HPS.UpdateButtonFill(stats);
        
        if (Foodtext != null)
        {
            foreach (var text in Foodtext)
            {
                if (text != null)
                    text.text = foodquantity.ToString() + "x";
            }
        }
    }

    private IEnumerator DecreaseFoodOnBackend()
    {
        string url = $"{apiUrlSecondary}/pets/{petId}/foodeat";

        UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Error updating food on backend: {request.error}");
        }
    }

    private void OnDrawGizmos()
    {
        if (showDebugInfo)
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
            
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Bounds expandedBounds = sr.bounds;
                expandedBounds.Expand(touchExpandArea);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(expandedBounds.center, expandedBounds.size);
            }
        }
    }
}