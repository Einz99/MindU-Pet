using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class CollarsMenu : MonoBehaviour
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
    public SoundManager SM;
    public GameObject Collar; // Add this to show/hide collar visual

    // Collar names
    private string[] collarName = new string[] { "RED COLLAR", "BLUE COLLAR", "GOLD COLLAR", "RAINBOW COLLAR" };

    private void Start()
    {
        // Initialize the bought array to false (no accessories are bought initially)
        bought = new bool[4];  // Assuming we have 4 collars (adjust size based on your actual accessory count)
        
        // Retrieve API URL and pet ID from PlayerPrefs
        string petKey = PlayerPrefKeys.PetPrefix;
        apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL);
        apiUrlSecondary = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary);
        petId = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);
        Debug.Log($"From Collars:\nRootAPI: {apiUrl}\nAPI: {apiUrlSecondary}\nPetID: {petId}");

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

                    // Check if the accessory is a "collar" and its ID is between 9 and 12
                    if (accessory.accessory_category == "collar" && accessory.accessory_id >= 9 && accessory.accessory_id <= 12)
                    {
                        // Determine the toggle and price panel based on the accessory_id
                        int toggleIndex = accessory.accessory_id - 9;  // Matching 0-based index for toggles array (9 -> 0, 10 -> 1, etc.)
                        int pricePanelIndex = accessory.accessory_id - 9;  // Matching 0-based index for price panel array (9 -> 0, 10 -> 1, etc.)

                        Debug.Log(accessory.accessory_category);
                        if (accessory.accessory_category != null) // This indicates the collar was bought
                        {
                            // Show the toggle for this collar and hide the price panel
                            toggles[toggleIndex].SetActive(true);
                            pricePanel[pricePanelIndex].SetActive(false);
                            
                            // Mark this accessory as bought in the bool array
                            bought[accessory.accessory_id - 9] = true;
                        }
                        else
                        {
                            // Hide the toggle for this collar and show the price panel
                            toggles[toggleIndex].SetActive(false);
                            pricePanel[pricePanelIndex].SetActive(true);

                            // Mark this accessory as not bought
                            bought[accessory.accessory_id - 9] = false;
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

    public void ShowConfirm(int collar)
    {
        // Check if the accessory (collar) has already been bought
        if (bought[collar])
        {
            Debug.Log("This accessory has already been bought.");
            return;  // Exit if the accessory is already bought
        }

        selected = collar;
        ConfirmPanel.SetActive(true);
        Confirmtext.text = $"Are you sure you want to buy {collarName[selected]} for 30 Coins?";  // Collars cost 30 coins
    }

    public void OnConfirm()
    {
        if(!gameObject.activeInHierarchy)
        {
            return;
        }
        ReduceCoinsAndUpdateAccessory();
    }

    private void ReduceCoinsAndUpdateAccessory()
    {
        string petKey = PlayerPrefKeys.PetPrefix;
        int currentCoins = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetCoins);
        int accessoryCost = 30;  // Collars cost 30 coins

        // Check if the player has enough coins
        if (currentCoins >= accessoryCost)
        {
            // Deduct the coins
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetCoins, currentCoins - accessoryCost);
            SM.PlayCoinSound();

            // Sync with the backend
            string url = $"{apiUrlSecondary}/pets/{petId}/buyAccessory"; // Use the secondary API
            string jsonData = JsonUtility.ToJson(new AccPayload { accessory_id = selected + 9 }); // Collar IDs are 9-12
            Debug.Log(url);
            
            // Create the HTTP request
            byte[] byteData = System.Text.Encoding.UTF8.GetBytes(jsonData);
            UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT)
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
            PlayerPrefs.SetInt(petkey + PlayerPrefKeys.PetNeck, selected + 9);  // Store collar selection in PlayerPrefs
            PlayerPrefs.Save();
            Collar.SetActive(true);
            
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
        ConfirmPanel.SetActive(false);
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
        if (toggles[index] == null)
        {
            Debug.LogError($"Toggle at index {index} is null!");
            return;
        }

        Toggle currentToggle = toggles[index].GetComponent<Toggle>();
        
        if (currentToggle == null)
        {
            Debug.LogError($"Toggle component at index {index} is null!");
            return;
        }
        
        string petKey = PlayerPrefKeys.PetPrefix;
        
        // If the toggle is turned off (i.e., the accessory is being removed)
        if (!currentToggle.isOn)
        {
            // Remove the accessory
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetNeck, 0);
            PlayerPrefs.Save();
            Collar.SetActive(true);
            
            // Call the backend API to remove the accessory (set to 0)
            StartCoroutine(UpdateAccessoryOnServer(0));
        }
        else
        {
            // Turn off all other toggles
            for (int i = 0; i < toggles.Length; i++)
            {
                if (i == index) continue;

                if (toggles[i] != null)
                {
                    Toggle otherToggle = toggles[i].GetComponent<Toggle>();
                    if (otherToggle != null)
                    {
                        otherToggle.isOn = false;
                    }
                }
            }
            
            // Set the selected accessory (9-12 for collars)
            int accessoryId = index + 9;
            
            // Update PlayerPrefs
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetNeck, accessoryId);
            PlayerPrefs.Save();
            Collar.SetActive(false);
            
            // Call the backend API to add the accessory
            StartCoroutine(UpdateAccessoryOnServer(accessoryId));
        }
    }

    private IEnumerator UpdateAccessoryOnServer(int accessory_id)
    {
        // The petId is stored as a PlayerPrefs value
        string url = $"{apiUrlSecondary}/pets/{petId}/accessory"; // API endpoint to update the accessory

        // Create the JSON payload
        string jsonData = JsonUtility.ToJson(new AccTogPayload { 
            accessory_id = accessory_id, 
            accessory_category = "collar" 
        });

        Debug.Log($"Updating accessory on server: ID={accessory_id}, URL={url}");

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
            // Optionally revert the PlayerPrefs change if the API call fails
        }
    }
}