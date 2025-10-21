using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class FoodMenu : MonoBehaviour
{
    private string apiUrl;
    private string apiUrlSecondary;
    private int petId;
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

    void Start()
    {
        string petKey = PlayerPrefKeys.PetPrefix;
        // Retrieve the API URL and pet ID from PlayerPrefs
        apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL, "http://192.168.1.5:3000");
        apiUrlSecondary = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary, apiUrl + "/api");
        petId = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);

        // Ensure that the "Not Enough Coins" panel is hidden by default
        notEnoughCoinsPanel.SetActive(false);
        
        string pet_type = PlayerPrefs.GetString(petKey + PlayerPrefKeys.PetType);

        // Set the sprite based on pet type (cat or dog)
        if (pet_type == "cat_1" || pet_type == "cat_2" || pet_type == "cat_3")
        {
            singleFood.sprite = foods[0];
            packFood.sprite = foods[1];  // Assign cat pack food sprite
            sft.text = "CAT FOOD";
            pft.text = "CAT PACK";
        }
        else
        {
            singleFood.sprite = foods[2];
            packFood.sprite = foods[3];  // Default to cat pack food sprite
            sft.text = "DOG FOOD";
            pft.text = "DOG PACK";
        }

        // Ensure that only the appropriate food sprites are active
        UpdateFoodSprites();
    }

    public void showConfirm(bool isPack)
    {
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
        // Calculate the coin deduction based on the increment value
        int coinDeduction = increment == 1 ? 10 : 35;

        // Check if the player has enough coins to proceed
        if (currentCoins >= coinDeduction)
        {
            // Deduct the coins locally
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetCoins, currentCoins - coinDeduction);

            SM.PlayCoinSound();

            // Add food to the local stack
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetFoodStack, currentFoodStack + increment);

            // Now, synchronize the backend with the updated data
            string url = $"{apiUrlSecondary}/pets/{petId}/food";
            
            // Create the JSON body data to send in the request
            string jsonData = JsonUtility.ToJson(new { increment = increment });

            // Convert the JSON string to a byte array
            byte[] byteData = System.Text.Encoding.UTF8.GetBytes(jsonData);

            // Make the PUT request to update the food quantity
            UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT)
            {
                uploadHandler = new UploadHandlerRaw(byteData),
                downloadHandler = new DownloadHandlerBuffer()
            };

            // Set the content type to application/json
            request.SetRequestHeader("Content-Type", "application/json");

            // Send the request and wait for a response
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Successfully updated, handle response
                Debug.Log("Food updated successfully: " + request.downloadHandler.text);
                PlayerPrefs.Save();

                
                foreach (var coin in Coins)
                {
                    coin.text = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetCoins).ToString();
                }
            }
            else
            {
                // Revert the local changes if the backend update fails
                Debug.LogError("Error updating food: " + request.error);
                PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetCoins, currentCoins); // Restore coins
                PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetFoodStack, currentFoodStack); // Restore food stack
                PlayerPrefs.Save();
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
    }
}
