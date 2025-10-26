using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;
using UnityEngine.Networking;

public class FoodButton : MonoBehaviour
{
    public PetBehaviour petBehaviour;
    public GameObject NotEnoughFoodPanel;
    public GameObject Foodtext;
    private InputAction clickAction;
    private InputAction positionAction; // Add this
    private bool isClickable = true;
    private int foodquantity;
    private string apiUrl;
    private string apiUrlSecondary;
    private int petId;
    public HorizontalPageScroller HPS;

    private void OnEnable()
    {
        // Initialize API URL and pet ID from PlayerPrefs
        string petKey = PlayerPrefKeys.PetPrefix;
        apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL);
        apiUrlSecondary = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary);
        petId = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);
        Debug.Log($"From FoodButton:\nRootAPI: {apiUrl}\nAPI: {apiUrlSecondary}\nPetID: {petId}");

        // Check if we are on mobile or desktop for input
        if (Application.isMobilePlatform)
        {
            clickAction = new InputAction(type: InputActionType.Button, binding: "<Touchscreen>/primaryTouch/press");
            positionAction = new InputAction(type: InputActionType.Value, binding: "<Touchscreen>/primaryTouch/position");
            clickAction.performed += OnClick;
            clickAction.Enable();
            positionAction.Enable();
        }
        else
        {
            clickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
            positionAction = new InputAction(type: InputActionType.Value, binding: "<Mouse>/position");
            clickAction.performed += OnClick;
            clickAction.Enable();
            positionAction.Enable();
        }
    }

    private void OnDisable()
    {
        clickAction.Disable();
        positionAction.Disable();
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        if (isClickable && IsSpriteClicked())
        {
            foodquantity = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetFoodStack);
            
            if (foodquantity == 0)
            {
                NotEnoughFoodPanel.SetActive(true);
                return;
            }

            petBehaviour.OnFeedButtonPressed();
            Foodtext.SetActive(false);

            StartCoroutine(DisableClickForDuration(10f));
            StartCoroutine(DecreaseFoodOnBackend());
        }
    }

    private bool IsSpriteClicked()
    {
        // Use the position action to read input position
        Vector2 inputPos = positionAction.ReadValue<Vector2>();
        
        Vector3 worldTouchPos = Camera.main.ScreenToWorldPoint(inputPos);
        worldTouchPos.z = 0;

        RaycastHit2D hit = Physics2D.Raycast(worldTouchPos, Vector2.zero);

        if (hit.collider != null && hit.collider.gameObject == gameObject)
        {
            return true;
        }

        return false;
    }

    private IEnumerator DisableClickForDuration(float duration)
    {
        isClickable = false;

        yield return new WaitForSeconds(duration);

        isClickable = true;
        Foodtext.SetActive(true);
        foodquantity -= 1;
        int playfulness = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetPlayfulness);
        int hunger = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHunger) + 30;
        int bath = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHygiene) - 2;  // Assuming bath is stored in hygiene
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
        Foodtext.GetComponent<TMP_Text>().text = foodquantity.ToString() + "x";
    }

    private IEnumerator DecreaseFoodOnBackend()
    {
        string url = $"{apiUrlSecondary}/pets/{petId}/foodeat";

        UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Food updated successfully on the backend.");
        }
        else
        {
            Debug.LogError("Error updating food on backend: " + request.error);
        }
    }
}