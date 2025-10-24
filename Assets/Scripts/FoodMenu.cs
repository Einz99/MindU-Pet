using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class FoodMenu : MonoBehaviour
{
    private bool isPack;

    // UI Elements
    public Image singleFood;  // Array to hold single food sprites
    public Image packFood; // For pack food sprite
    public Sprite[] foods = new Sprite[4];  // Single array for both cat and dog food sprites
    public TMP_Text sft;
    public TMP_Text pft;
    public GameObject notEnoughCoinsPanel;
    public GameObject ConfirmPanel;
    public TMP_Text Confirmtext;
    public Button ConfirmTransact;
    public TMP_Text[] Coins;
    public SoundManager SM;
    public TMP_Text Foodquantity;

    void OnEnable()
    {
        // This will be called when the GameObject is activated or enabled
        UpdateFoodSprites();  // Update the sprite whenever the GameObject is activated
    }

    public void showConfirm(bool isPack)
    {
        string pet_type = PlayerPrefs.GetString(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetType);
        Debug.Log(pet_type);
        ConfirmPanel.SetActive(true);
        this.isPack = isPack;
        if (isPack)
        {
            Confirmtext.text = "Are you sure you want to buy CAT PACK for 35 Coins?";
        } else
        {
            Confirmtext.text = "Are you sure you want to buy CAT PACK for 10 Coins?";
        }
    }

    public void OnConfirm()
    {
        if(!gameObject.activeInHierarchy)
        {
            return;
        }
        if(isPack)
        {
            AddFood5();
        } else
        {
            AddFood1();
        }
    }

    // Function to add 1 food to the pet
    private void AddFood1()
    {
        StartCoroutine(UpdateFoodQuantity(1));
    }

    // Function to add 5 food to the pet
    private void AddFood5()
    {
        StartCoroutine(UpdateFoodQuantity(5));
    }

    // Update food quantity (1 or 5 increment)
    private IEnumerator UpdateFoodQuantity(int increment)
{
    string petKey = PlayerPrefKeys.PetPrefix;
    int currentCoins = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetCoins);
    int currentFoodStack = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetFoodStack);
    Debug.Log(currentCoins);
    
    // Ensure that the increment value is valid
    if (increment != 1 && increment != 5)
    {
        Debug.LogError("Invalid increment value. It should be 1 or 5.");
        yield break;  // Exit the coroutine if the increment is invalid
    }

    // Calculate the coin deduction based on the increment value
    int coinDeduction = increment == 1 ? 10 : 35;

    if (currentCoins >= coinDeduction)
    {
        // Deduct coins locally
        PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetCoins, currentCoins - coinDeduction);
        SM.PlayCoinSound();

        // Add food to the local stack
        PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetFoodStack, currentFoodStack + increment);

        string apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL);
        string apiUrlSecondary = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary, apiUrl + "/api");
        int petId = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);

        // Construct the API URL for updating food
        string url = $"{apiUrlSecondary}/pets/{petId}/food";

        // Create the payload with correct JSON format
        foodbuyPayload payload = new foodbuyPayload { increments = increment };
        string jsonData = JsonUtility.ToJson(payload);  // Serialize the payload into JSON

        // Create the UnityWebRequest to make the PUT request
        UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT)
        {
            uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData)),
            downloadHandler = new DownloadHandlerBuffer()
        };

        // Set the content type header
        request.SetRequestHeader("Content-Type", "application/json");

        // Send the request and wait for a response
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
                Debug.Log("Food updated successfully: " + request.downloadHandler.text);
                ConfirmPanel.SetActive(false);
            PlayerPrefs.Save();

            // Update the displayed coin amount
            foreach (var coin in Coins)
            {
                coin.text = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetCoins).ToString();
            }
        }
        else
        {
            Debug.LogError("Error updating food: " + request.error);
            // Revert the local changes if the backend update fails
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetCoins, currentCoins); // Restore coins
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetFoodStack, currentFoodStack); // Restore food stack
                PlayerPrefs.Save();
            ConfirmPanel.SetActive(false);
        }
    }
    else
    {
        // Show the "Not Enough Coins" panel
        ShowNotEnoughCoinsPanel();
    }
}

    // Function to show the "Not Enough Coins" panel
    private void ShowNotEnoughCoinsPanel()
    {
        notEnoughCoinsPanel.SetActive(true);

        // Disable the buttons so the player can't attempt to add food
        ConfirmTransact.interactable = true;
    }

    // Function to close the "Not Enough Coins" panel
    public void CloseNotEnoughCoinsPanel()
    {
        notEnoughCoinsPanel.SetActive(false);

        // Re-enable the buttons so the player can try again if needed
        ConfirmTransact.interactable = true;
    }

    // Method to update the visibility of the food sprites
    private void UpdateFoodSprites()
    {
        // Enable the appropriate food sprites (singleFood and packFood) based on the condition
        singleFood.enabled = true;
        packFood.enabled = true;
        string petKey = PlayerPrefKeys.PetPrefix;

        string pet_type = PlayerPrefs.GetString(petKey + PlayerPrefKeys.PetType);
        Debug.Log(pet_type);

        if (pet_type == "cat_1" || pet_type == "cat_2" || pet_type == "cat_3")
        {
            singleFood.sprite = foods[0];  // Assign cat single food sprite
            packFood.sprite = foods[1];   // Assign cat pack food sprite

            sft.text = "CAT FOOD";
            pft.text = "CAT PACK";
        }
        else
        {
            singleFood.sprite = foods[2];  // Assign dog single food sprite
            packFood.sprite = foods[3];   // Assign dog pack food sprite

            sft.text = "DOG FOOD";
            pft.text = "DOG PACK";
        }
    }
}

[System.Serializable]
public class foodbuyPayload
{
    public int increments;  // The increment value for playfulness
}