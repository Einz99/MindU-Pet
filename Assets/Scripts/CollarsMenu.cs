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
    private bool isTogglingProgrammatically = false;
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

                // Convert the response into a structured object (this depends on your JSON structure).
                Accessory[] accessories = JsonUtility.FromJson<AccessoryList>("{\"items\":" + responseText + "}").items;

                // Check if no accessories are returned
                if (accessories == null || accessories.Length == 0)
                {
                    
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
            ConfirmPanel.SetActive(false);
            ShowNotEnoughCoinsPanel();
        }
    }

    private IEnumerator SendRequest(UnityWebRequest request, int currentCoins)
    {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string petkey = PlayerPrefKeys.PetPrefix;

            bought[selected] = true;

            isTogglingProgrammatically = true;

            for (int i = 0; i < toggles.Length; i++)
            {
                if (toggles[i] != null)
                {
                    Toggle toggle = toggles[i].GetComponent<Toggle>();
                    if (toggle != null)
                    {
                        toggle.isOn = false;
                        toggle.interactable = true;
                    }
                }
            }

            toggles[selected].SetActive(true);

            Toggle purchasedToggle = toggles[selected].GetComponent<Toggle>();
            if (purchasedToggle != null)
            {
                purchasedToggle.isOn = true;
                purchasedToggle.interactable = true;
            }

            isTogglingProgrammatically = false;

            pricePanel[selected].SetActive(false);

            PlayerPrefs.SetInt(petkey + PlayerPrefKeys.PetNeck, selected + 9);
            PlayerPrefs.Save();
            StartCoroutine(UpdateAccessoryOnServer(selected + 9));

            Collar.SetActive(true);

            foreach (var coin in Coins)
            {
                coin.text = PlayerPrefs.GetInt(petkey + PlayerPrefKeys.PetCoins).ToString();
            }
        }
        else
        {
            Debug.LogError("Error purchasing accessory: " + request.error);
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
        // Turn off all toggles but keep bought ones visible (just unchecked)
        foreach (var toggle in toggles)
        {
            if (toggle != null)
            {
                Toggle toggleComponent = toggle.GetComponent<Toggle>();
                if (toggleComponent != null)
                {
                    toggleComponent.isOn = false;
                    toggleComponent.interactable = true;
                }
            }
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
        // Ignore programmatic toggle changes
        if (isTogglingProgrammatically) return;

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

        // If the toggle is turned off (removing the accessory)
        if (!currentToggle.isOn)
        {
            // Remove the accessory
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetNeck, 0);
            PlayerPrefs.Save();

            // Hide collar when toggle is OFF
            Collar.SetActive(false);

            // Call the backend API to remove the accessory (set to 0)
            StartCoroutine(UpdateAccessoryOnServer(0));
        }
        else
        {
            // Set flag to prevent other toggles from triggering callbacks
            isTogglingProgrammatically = true;

            // Turn off all other toggles
            for (int i = 0; i < toggles.Length; i++)
            {
                if (i == index) continue;

                if (toggles[i] != null && toggles[i].activeInHierarchy)
                {
                    Toggle otherToggle = toggles[i].GetComponent<Toggle>();
                    if (otherToggle != null)
                    {
                        otherToggle.isOn = false; // Won't trigger OnToggle
                    }
                }
            }

            // Reset flag
            isTogglingProgrammatically = false;

            // Set the selected accessory (9-12 for collars)
            int accessoryId = index + 9;

            // Update PlayerPrefs
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetNeck, accessoryId);
            PlayerPrefs.Save();

            // Show collar when toggle is ON
            Collar.SetActive(true);

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


        // Create the UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT)
        {
            uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Content-Type", "application/json");

        // Send the request and wait for a response
        yield return request.SendWebRequest();
    }
}