using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;  // For Coroutine
using UnityEngine.Networking;  // For API calls

public class FoodButton : MonoBehaviour
{
    public PetBehaviour petBehaviour;
    public GameObject NotEnoughFoodPanel;
    public GameObject Foodtext;
    private InputAction clickAction;
    private bool isClickable = true;  // Flag to check if the sprite is clickable
    private int foodquantity;
    private string apiUrl;
    private string apiUrlSecondary;
    private int petId;

    private void OnEnable()
    {
        // Initialize API URL and pet ID from PlayerPrefs
        string petKey = PlayerPrefKeys.PetPrefix;
        apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL, "http://192.168.1.5:3000");
        apiUrlSecondary = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary, apiUrl + "/api");
        petId = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);

        // Check if we are on mobile or desktop for input
        if (Application.isMobilePlatform)
        {
            clickAction = new InputAction(type: InputActionType.Button, binding: "<Touchscreen>/primaryTouch");
            clickAction.performed += OnClick;
            clickAction.Enable();
        }
        else
        {
            clickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
            clickAction.performed += OnClick;
            clickAction.Enable();
        }
    }

    private void OnDisable()
    {
        // Disable the click action when this object is disabled
        clickAction.Disable();
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        // Check if the click is within the bounds of the sprite (e.g., the food button)
        if (isClickable && IsSpriteClicked())  // Only proceed if it's clickable and sprite is clicked
        {
            foodquantity = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetFoodStack);
            
            if (foodquantity == 0)
            {
                NotEnoughFoodPanel.SetActive(true);
                return;
            }

            // Trigger the feeding action (for UI interaction)
            petBehaviour.OnFeedButtonPressed();
            Foodtext.SetActive(false);

            // Make the sprite unclickable for 10 seconds
            StartCoroutine(DisableClickForDuration(10f));

            // Call the API to decrease food on the backend
            StartCoroutine(DecreaseFoodOnBackend());
            DataManager.Instance.StartCoroutine(DataManager.Instance.HandlePetDataRequest());
            int playfulness = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetPlayfulness);
            int hunger = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHunger);
            int bath = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHygiene); // Assuming bath is stored in hygiene
            int sleep = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetSleep);

            // Create an array with the stats
            int[] stats = new int[] { playfulness, hunger, bath, sleep };
            // Call UpdateButtonFill with the stats array
            HorizontalPageScroller.Instance.UpdateButtonFill(stats);
        }
    }

    private bool IsSpriteClicked()
    {
        // Use raycasting to detect if the sprite (Collider2D) is clicked
        Vector2 inputPos;

        if (Application.isMobilePlatform)
        {
            inputPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else
        {
            inputPos = Mouse.current.position.ReadValue();
        }

        Vector3 worldTouchPos = Camera.main.ScreenToWorldPoint(inputPos);
        worldTouchPos.z = 0;

        // Raycast to check if the touch or mouse is within the bounds of the sprite collider
        RaycastHit2D hit = Physics2D.Raycast(worldTouchPos, Vector2.zero);

        if (hit.collider != null && hit.collider.gameObject == gameObject)  // Only proceed if the click is on the sprite
        {
            return true;
        }

        return false;
    }

    private IEnumerator DisableClickForDuration(float duration)
    {
        isClickable = false;  // Disable clicking

        // Wait for the specified duration
        yield return new WaitForSeconds(duration);

        // Re-enable clicking and update food text
        isClickable = true;
        Foodtext.SetActive(true);
        foodquantity -= 1;
        Foodtext.GetComponent<TMP_Text>().text = foodquantity.ToString() + "x";
        PlayerPrefs.SetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetFoodStack, foodquantity);
        PlayerPrefs.Save();
    }

    private IEnumerator DecreaseFoodOnBackend()
    {
        // Prepare the request URL
        string url = $"{apiUrlSecondary}/pets/{petId}/foodeat";

        // Create a PUT request to decrease the food on the backend
        UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT);
        request.SetRequestHeader("Content-Type", "application/json");

        // Wait for the request to complete
        yield return request.SendWebRequest();

        // Handle the response
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
