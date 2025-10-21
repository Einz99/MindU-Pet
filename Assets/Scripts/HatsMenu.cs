using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class HatsMenu : MonoBehaviour
{
    private string apiUrl;
    private string apiUrlSecondary;
    private int petId;

    public GameObject[] toggles;  // GameObjects to display when accessories are available
    public GameObject[] pricePanel;  // GameObjects to hide when accessories are bought
    private bool[] bought;  // Array to track which accessories have been bought (true = bought, false = not bought)
    private int selected;
    public GameObject notEnoughCoinsPanel;
    public GameObject ConfirmPanel;
    public TMP_Text Confirmtext;
    public Button ConfirmTransact;
    public TMP_Text[] Coins;
    private string[] hatsName = new string[] { "CAP", "COWBOY", "WITCH", "BEANIE" };
    public SoundManager SM;

    private void Start()
    {
        // Initialize the bought array to false (no accessories are bought initially)
        bought = new bool[4];  // Assuming we have 4 hats (adjust size based on your actual accessory count)
        
        // Retrieve API URL and pet ID from PlayerPrefs
        string petKey = PlayerPrefKeys.PetPrefix;
        apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL, "http://192.168.1.5:3000");
        apiUrlSecondary = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary, apiUrl + "/api");
        petId = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);

        // Call the method to fetch accessories
        StartCoroutine(GetAccessories());
    }

    // Coroutine to fetch accessories for the pet
    private IEnumerator GetAccessories()
    {
        string url = apiUrlSecondary + "/pets/" + petId + "/accessories";  // API URL

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Send the request and wait for the response
            yield return request.SendWebRequest();

            // Handle the response
            if (request.result == UnityWebRequest.Result.Success)
            {
                // Parse the response if successful
                string responseText = request.downloadHandler.text;
                Debug.Log("Response: " + responseText);

                // Convert the response into a structured object (this depends on your JSON structure).
                Accessory[] accessories = JsonUtility.FromJson<AccessoryList>("{\"items\":" + responseText + "}").items;

                // Check if no accessories are returned
                if (accessories == null || accessories.Length == 0)
                {
                    // Handle the case where there are no accessories
                    Debug.Log("No accessories found.");
                    
                    // Reset all toggles and price panels to the default state (price panels active, toggles inactive)
                    HideAllToggles();
                    ShowAllPricePanels();
                    
                    // Reset the bought array to false (no accessories are bought)
                    for (int i = 0; i < bought.Length; i++)
                    {
                        bought[i] = false;
                    }

                    yield break; // Exit the coroutine as no accessories are available
                }

                // Loop through each accessory and determine its state
                for (int i = 0; i < accessories.Length; i++)
                {
                    Accessory accessory = accessories[i];

                    // Check if the accessory is a "hat" and its ID is between 1 and 4
                    if (accessory.accessory_category == "hat" && accessory.accessory_id >= 1 && accessory.accessory_id <= 4)
                    {
                        // Determine the toggle and price panel based on the accessory_id
                        int toggleIndex = accessory.accessory_id - 1;  // Matching 0-based index for toggles array
                        int pricePanelIndex = accessory.accessory_id - 1;  // Matching 0-based index for price panel array

                        if (accessory.accessory_type != null) // This indicates the hat was bought
                        {
                            // Show the toggle for this hat and hide the price panel
                            toggles[toggleIndex].SetActive(true);
                            pricePanel[pricePanelIndex].SetActive(false);
                            
                            // Mark this accessory as bought in the bool array
                            bought[accessory.accessory_id - 1] = true;
                        }
                        else
                        {
                            // Hide the toggle for this hat and show the price panel
                            toggles[toggleIndex].SetActive(false);
                            pricePanel[pricePanelIndex].SetActive(true);

                            // Mark this accessory as not bought
                            bought[accessory.accessory_id - 1] = false;
                        }
                    }
                }
            }
            else
            {
                // Handle error
                Debug.LogError("Error: " + request.error);
            }
        }
    }

    public void ShowConfirm(int hat)
{
    // Check if the accessory has already been bought
    if (bought[hat])
    {
        Debug.Log("This accessory has already been bought.");
        return;  // Exit if the accessory is already bought
    }

    selected = hat;
    ConfirmPanel.SetActive(true);
    Confirmtext.text = $"Are you sure you want to buy {hatsName[selected]} for 40 Coins?";
}
    
    public void OnConfirm()
    {
        ReduceCoinsAndUpdateAccessory();
    }
    
    private void ReduceCoinsAndUpdateAccessory()
    {
        string petKey = PlayerPrefKeys.PetPrefix;
        int currentCoins = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetCoins);
        int accessoryCost = 40;  // Assuming hats cost 40 coins (change as needed)
        string accessoryType = hatsName[selected]; // Get the correct accessory name based on selection
    
        // Check if the player has enough coins
        if (currentCoins >= accessoryCost)
        {
            // Deduct the coins
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetCoins, currentCoins - accessoryCost);

            SM.PlayCoinSound();
            // Sync with the backend
            string url = $"{apiUrlSecondary}/pets/{petId}/buy-accessory"; // Use the secondary API
            string jsonData = JsonUtility.ToJson(new { accessory_id = selected + 1 }); // Assuming `selected` corresponds to the accessory ID
    
            // Create the HTTP request
            byte[] byteData = System.Text.Encoding.UTF8.GetBytes(jsonData);
            UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(byteData),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
    
            StartCoroutine(SendRequest(request, currentCoins));
        }
        else
        {
            // Show "Not Enough Coins" panel if the player does not have enough coins
            ShowNotEnoughCoinsPanel();
        }
    }
    
    private IEnumerator SendRequest(UnityWebRequest request, int currentCoins)
    {
        yield return request.SendWebRequest();
    
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Accessory purchased successfully: " + request.downloadHandler.text);
            string petkey = PlayerPrefs.GetString(PlayerPrefKeys.PetPrefix);
            PlayerPrefs.SetInt(petkey + PlayerPrefKeys.PetHead, selected + 1);
            PetBehaviour.Instance.reflectPetData();
            PlayerPrefs.Save();
        
            // Mark the accessory as bought
            bought[selected] = true;
    
            // Deactivate all toggles and show the selected one
            DeactivateAllToggles();
            toggles[selected].SetActive(true);
    
            // Hide the price panel for the purchased accessory
            pricePanel[selected].SetActive(false);

            // Enable the purchased toggle (set it to "on")
            OnToggle(selected);
    
            // Update the displayed coin amount
            string petKey2 = PlayerPrefKeys.PetPrefix;
            foreach (var coin in Coins)
            {
                coin.text = PlayerPrefs.GetInt(petKey2 + PlayerPrefKeys.PetCoins).ToString();
            }
        }
        else
        {
            Debug.LogError("Error purchasing accessory: " + request.error);
            // Revert coin deduction if API call fails
            string petKey = PlayerPrefKeys.PetPrefix;
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetCoins, currentCoins);
            PlayerPrefs.Save();
        }
    }
    
    private void ShowNotEnoughCoinsPanel()
    {
        notEnoughCoinsPanel.SetActive(true);
    }
    
    private void DeactivateAllToggles()
    {
        // Deactivate all toggles and make them interactable
        foreach (var toggle in toggles)
        {
            toggle.SetActive(false);
            toggle.GetComponent<Toggle>().interactable = true;  // Ensure all toggles are interactable again
            toggle.GetComponent<Toggle>().isOn = false;  // Make sure all toggles are turned off
        }
    }
    
    // Hide all toggles
    private void HideAllToggles()
    {
        foreach (var toggle in toggles)
        {
            toggle.SetActive(false);
        }
    }
    
    // Show all price panels
    private void ShowAllPricePanels()
    {
        foreach (var panel in pricePanel)
        {
            panel.SetActive(true);
        }
    }
    
    // Called when a toggle is clicked
    public void OnToggle(int index)
    {
        // If the toggle is turned off (i.e., the accessory is being removed)
        if (!toggles[index].GetComponent<Toggle>().isOn)
        {
            // Mark the accessory as not worn
            bought[index] = false;

            // Hide the selected accessory's toggle and show the price panel again
            toggles[index].SetActive(false);
            pricePanel[index].SetActive(true);

            // Call the backend API to remove the accessory
            StartCoroutine(UpdateAccessoryOnServer(0)); // Passing 0 to remove the accessory
        }
        else
        {
            // If the toggle is turned on (i.e., the accessory is being worn)
            DeactivateAllToggles();
            toggles[index].SetActive(true);  // Only the selected toggle is shown
            pricePanel[index].SetActive(false);  // Hide price panel for the selected accessory

            // Call the backend API to add the accessory
            StartCoroutine(UpdateAccessoryOnServer(selected + 1)); // Passing the accessory_id to wear
        }
    }

    private IEnumerator UpdateAccessoryOnServer(int accessory_id)
    {
        // The petId is stored as a PlayerPrefs value
        string url = $"{apiUrlSecondary}/pets/{petId}/accessory"; // API endpoint to update the accessory
        string jsonData = JsonUtility.ToJson(new { accessory_id = accessory_id, accessory_category = "hat" }); // Create the JSON payload

        // Create the UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT)
        {
            uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Content-Type", "application/json");

        // Send the request and wait for a response
        yield return request.SendWebRequest();

        // Handle the response
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Accessory updated successfully: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Error updating accessory: " + request.error);
        }
    }
}

// Accessory data structure to hold the response data
[System.Serializable]
public class Accessory
{
    public int accessory_id;
    public string accessory_category;
    public string accessory_type; // This will be used to determine if the accessory is bought (non-null means bought)
}

// Wrapper class for a list of accessories
[System.Serializable]
public class AccessoryList
{
    public Accessory[] items;
}
