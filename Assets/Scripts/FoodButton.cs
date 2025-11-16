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
    public float touchExpandArea = 0.5f; // Expand clickable area for easier tapping
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool forceWebGLMode = false; // Force WebGL behavior in editor for testing

    private void Start()
    {
        // Initialize API URL and pet ID from PlayerPrefs
        string petKey = PlayerPrefKeys.PetPrefix;
        apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL);
        apiUrlSecondary = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary);
        petId = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);
        
        LogPlatformInfo();

        // Ensure collider exists for WebGL touch
        EnsureCollider();
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
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            // Add BoxCollider2D if none exists
            BoxCollider2D boxCol = gameObject.AddComponent<BoxCollider2D>();
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // Expand the collider slightly for easier tapping
                boxCol.size = sr.bounds.size + new Vector3(touchExpandArea, touchExpandArea, 0);
            }
        }
    }

    // Method 1: Unity's built-in mouse events (works in Unity Editor & PC builds)
    private void OnMouseDown()
    {
#if UNITY_EDITOR
        // In Unity Editor, use OnMouseDown for easy testing
        if (!forceWebGLMode)
        {
            if (!isClickable) return;

            // Ignore if clicking on UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            HandleFoodClick();
        }
#elif UNITY_WEBGL
        // In WebGL build, OnMouseDown works well
        if (!isClickable) return;

        // Ignore if clicking on UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        HandleFoodClick();
#endif
    }

    // Method 2: Update-based touch detection (for mobile & WebGL testing)
    private void Update()
    {
        // Only process if clickable
        if (!isClickable) return;

#if UNITY_EDITOR
        // In Unity Editor with forceWebGLMode, simulate mobile touch behavior
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
        // Handle touch input for mobile/WebGL
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            // Only process on TouchPhase.Began to avoid multiple triggers
            if (touch.phase == TouchPhase.Began)
            {
                if (IsTouchOnSprite(touch.position))
                {
                    HandleFoodClick();
                }
            }
        }
        // Fallback to mouse for WebGL on desktop
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
        // Ignore if clicking on UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }

        // Convert screen position to world position
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPosition);
        worldPos.z = 0;

        // Method 1: Use Collider2D.OverlapPoint (most reliable)
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && col.OverlapPoint(worldPos))
        {
            return true;
        }

        // Method 2: Raycast (fallback)
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
        if (hit.collider != null && hit.collider.gameObject == gameObject)
        {
            return true;
        }

        // Method 3: Bounds check with expanded area (most forgiving)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Bounds bounds = sr.bounds;
            // Expand bounds for easier tapping
            bounds.Expand(touchExpandArea);
            
            if (bounds.Contains(worldPos))
            {
                return true;
            }
        }

        return false;
    }

    private void HandleFoodClick()
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
        
        // Create an array with the updated stats
        int[] stats = new int[] { playfulness, hunger, bath, sleep };

        // Call UpdateButtonFill with the stats array
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

    // Visualize the clickable area in editor
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