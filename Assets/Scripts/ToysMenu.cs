using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class ToysMenu : MonoBehaviour
{
    private string apiUrl;
    private string apiUrlSecondary;
    private int petId;
    private int selectedToyIndex;
    private int[] prices = new int[] { 20, 25, 30, 35, 40 };
    private string[] toyname = new string[] { "Ball Toy", "Small Ball", "Bone Toy", "Plush Toy", "Squeeky Toy" };
    private string[] toy_types = new string[] { "toy_2", "toy_3", "toy_4", "toy_5", "toy_6" };
    public GameObject[] Toggles = new GameObject[6];  // Toggle elements for toys
    public GameObject[] PriceTag = new GameObject[5];  // Price tags for toys
    public GameObject[] Toys = new GameObject[6];
    public Button[] Selections = new Button[6];
    public TMP_Text Confirmtext;
    public Button ConfirmTransact;
    public TMP_Text[] Coins;
    public GameObject ConfirmPanel;
    public GameObject notEnoughCoinsPanel;
    public SoundManager SM;

    private bool[] toggleStatus = new bool[6];
    private bool isUpdatingToggle = false;  // Guard flag to prevent recursion
    
    private List<string> toys = new List<string>();

    private void Start() 
    {
        string petKey = PlayerPrefKeys.PetPrefix;
        apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL);
        apiUrlSecondary = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary);
        petId = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);


        // Initialize toggle status from PlayerPrefs
        for (int i = 0; i < toggleStatus.Length; i++)
        {
            toggleStatus[i] = PlayerPrefs.GetInt(PlayerPrefKeys.toyPrefix + i, 0) == 1;
            // Only set the toggle if it's active (meaning the toy is owned)
            if (Toggles[i].activeInHierarchy)
            {
                Toggles[i].GetComponent<Toggle>().SetIsOnWithoutNotify(toggleStatus[i]);  // Fixed: prevents callback trigger
            }
        }

        StartCoroutine(GetToysData());
    }

    private IEnumerator GetToysData() {
        string url = $"{apiUrlSecondary}/pets/{petId}/toy";  // Construct the API URL
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url)) {
            // Send the request and wait for a response
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success) {
                // Parse the response
                string jsonResponse = webRequest.downloadHandler.text;
                List<string> toys = ParseToysData(jsonResponse);  // Parse the toy data

                // Update the UI based on the returned toys
                UpdateUIBasedOnToys(toys);
            } else {
                Debug.LogError("Error retrieving toys data: " + webRequest.error);
            }
        }
    }

    // Method to parse the toy data from the response
    private List<string> ParseToysData(string jsonResponse)
    {
        // Deserialize the JSON response into a ToyList object
        ToyList toyData = JsonUtility.FromJson<ToyList>("{\"toys\":" + jsonResponse + "}");  // Wrap the array in a "toys" field

        // Loop through all the toys and add the toy_type to the list
        foreach (Toy toy in toyData.toys)
        {
            toys.Add(toy.toy_type);
        }

        return toys;
    }

    // Method to update the UI based on the available toys
    private void UpdateUIBasedOnToys(List<string> toys)
    {

        for (int i = 0; i < 6; i++)
        {
            string toyType = "toy_" + (i + 1); // Generate toy type name (toy_1, toy_2, etc.)

            // If the current toy type exists in the list of toys, activate the corresponding toggle
            if (toys.Contains(toyType))
            {
                Toggles[i].SetActive(true);  // Show the toggle for this toy
                Selections[i].interactable = false;
                if (i != 0)
                {
                    i--;
                    PriceTag[i].SetActive(false); // Hide the price tag if the toy is available
                    i++;
                }

                // Optionally, check if the toggle is turned on from PlayerPrefs and set the toggle state
                bool isToggleOn = PlayerPrefs.GetInt(PlayerPrefKeys.toyPrefix + i, 0) == 1;
                Toggles[i].GetComponent<Toggle>().SetIsOnWithoutNotify(isToggleOn);  // Fixed: prevents callback trigger
            }
            else
            {
                Toggles[i].SetActive(false); // Hide the toggle if the toy doesn't exist
                if (i < 5)
                {
                    PriceTag[i].SetActive(true); // Show the price tag if the toy doesn't exist 
                }
            }
        }

        // Save preferences
        PlayerPrefs.Save();
    }

    public void showConfirm(int selected)
    {
        selectedToyIndex = selected;
        Confirmtext.text = $"Are you sure you want to buy {toyname[selected - 1]} for {prices[selected - 1]} Coins?";

        // Show the confirmation panel
        ConfirmPanel.SetActive(true);
    }

    public void OnConfirm()
    {
        if (gameObject.activeInHierarchy)
        {
            ReduceCoinsAndUpdateToy();   
        }
    }

    private void ReduceCoinsAndUpdateToy()
    {
        string petKey = PlayerPrefKeys.PetPrefix;
        int currentCoins = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetCoins);
        int toyCost = prices[selectedToyIndex - 1];
        string toyTypes = toy_types[selectedToyIndex - 1];

        if (currentCoins >= toyCost)
        {
            SM.PlayCoinSound();
            // Deduct the coins
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetCoins, currentCoins - toyCost);

            // Sync with the backend
            string url = $"{apiUrlSecondary}/pets/{petId}/buyToy"; // Use the secondary API
            string jsonData = JsonUtility.ToJson(new ToyPayload { toy_type = toyTypes });

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
        // Send the request to the server
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Parse the JSON response from the server
            string jsonResponse = request.downloadHandler.text;
            var response = JsonUtility.FromJson<ToyPurchaseResponse>(jsonResponse);

            // Check if the transaction was successful and update the UI
            if (response != null)
            {
                // Update the player's coin count and toy status
                PlayerPrefs.SetInt(PlayerPrefKeys.PetCoins, response.new_coins);
                PlayerPrefs.SetString(PlayerPrefKeys.toyPrefix + selectedToyIndex, response.toy_type);

                // Update the UI with the new toy data
                ConfirmPanel.SetActive(false); // Hide the confirmation panel
                SM.PlayCoinSound(); // Play sound on successful transaction
                TogglingToys(selectedToyIndex);
                // Show success message (can be customized based on your requirements)

                // Optionally, you can update the player's toy UI or handle the UI transition here
                // For example, update the toggle and price tag display based on new toy data
                toys.Add(response.toy_type);
                UpdateUIBasedOnToys(toys);
            }
        }
        else
        {
            // Show error message if the request failed
            Debug.LogError("Error purchasing toy: " + request.error);
            ConfirmPanel.SetActive(false);
        }
    }

    private void ShowNotEnoughCoinsPanel()
    {
        notEnoughCoinsPanel.SetActive(true);
    }
    
    public void TogglingToys(int index)
    {
        // Prevent recursion
        if (isUpdatingToggle) return;
        
        isUpdatingToggle = true;
        
        // Get current toggle state
        bool currentStatus = toggleStatus[index];

        // Flip the status
        bool newStatus = !currentStatus;

        // Save the new status
        toggleStatus[index] = newStatus;
        PlayerPrefs.SetInt(PlayerPrefKeys.toyPrefix + index, newStatus ? 1 : 0);

        // Update the UI WITHOUT triggering the onValueChanged callback
        Toggle toggle = Toggles[index].GetComponent<Toggle>();
        toggle.SetIsOnWithoutNotify(newStatus);  // Fixed: prevents infinite recursion
        
        Toys[index].SetActive(newStatus);

        // Save changes
        PlayerPrefs.Save();
        
        isUpdatingToggle = false;
    }
}

// Response structure for the toy purchase response
[System.Serializable]
public class Toy
{
    public int pet_id;
    public string toy_type;
    public int is_active;  // 1 if active, 0 if not
}

// Class to deserialize the JSON response (example structure)
[System.Serializable]
public class ToyList
{
    public List<Toy> toys;  // List of Toy objects for the pet
}

[System.Serializable]
public class ToyPurchaseResponse
{
    public string toy_type;
    public int new_coins;
}

[System.Serializable]
public class ToyPayload
{
    public string toy_type;
}