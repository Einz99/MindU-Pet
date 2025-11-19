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
                
                // Add BoxCollider2D if none exists
                BoxCollider2D boxCol = gameObject.AddComponent<BoxCollider2D>();
                
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    // FIX: Convert Vector3 to Vector2 properly
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

    // Method 1: Unity's built-in mouse events
    private void OnMouseDown()
    {
#if UNITY_EDITOR
        if (!forceWebGLMode)
        {
            if (!isClickable) return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            HandleFoodClick();
        }
#elif UNITY_WEBGL
        if (!isClickable) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        HandleFoodClick();
#endif
    }

    // Method 2: Update-based touch detection
    private void Update()
    {
        if (!isClickable) return;

#if UNITY_EDITOR
        if (forceWebGLMode)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (IsTouchOnSprite(Input.mousePosition))
                {
                    HandleFoodClick();
                }
            }
        }
#elif UNITY_WEBGL || UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                if (IsTouchOnSprite(touch.position))
                {
                    HandleFoodClick();
                }
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            if (IsTouchOnSprite(Input.mousePosition))
            {
                HandleFoodClick();
            }
        }
#endif
    }

    private bool IsTouchOnSprite(Vector2 screenPosition)
    {
        try
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return false;
            }

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPosition);
            worldPos.z = 0;

            // Method 1: Use Collider2D
            Collider2D col = GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(worldPos))
            {
                return true;
            }

            // Method 2: Raycast
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                return true;
            }

            // Method 3: Bounds check
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Bounds bounds = sr.bounds;
                bounds.Expand(touchExpandArea);
                
                if (bounds.Contains(worldPos))
                {
                    return true;
                }
            }

            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Error in IsTouchOnSprite: {ex.Message}");
            return false;
        }
    }

    private void HandleFoodClick()
    {
        try
        {
            foodquantity = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetFoodStack);

            if (foodquantity == 0)
            {
                NotEnoughFoodPanel.SetActive(true);
                return;
            }

            petBehaviour.OnFeedButtonPressed();
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
        HPS.UpdateButtonFill(stats);
        
        foreach (var text in Foodtext)
        {
            text.text = foodquantity.ToString() + "x";
        }
    }

    private IEnumerator DecreaseFoodOnBackend()
    {
        string url = $"{apiUrlSecondary}/pets/{petId}/foodeat";

        UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
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