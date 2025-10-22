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
    public TMP_Text Confirmtext;
    public Button ConfirmTransact;
    public TMP_Text[] Coins;
    public GameObject ConfirmPanel;
    public GameObject notEnoughCoinsPanel;
    public SoundManager SM;

    private bool[] toggleStatus = new bool[6];
    

    private void Start() {
        string petKey = PlayerPrefKeys.PetPrefix;
        // Retrieve the API URL and pet ID from PlayerPrefs
        apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL, "http://192.168.1.5:3000");
        apiUrlSecondary = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary, apiUrl + "/api");
        petId = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);

        // Start the coroutine to get toys data from the API
        StartCoroutine(GetToysData());

        bool status;
        for (int i = 0; i < toggleStatus.Length; i++)
        {
            status = PlayerPrefs.GetInt(PlayerPrefKeys.toyPrefix + i) == 1;
            toggleStatus[i] = status;
            Toggles[i].GetComponent<Toggle>().isOn = status;
        }
        
    }

    private IEnumerator GetToysData() {
        string url = $"{apiUrlSecondary}/pets/{petId}/toys";  // Construct the API URL
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
    private List<string> ParseToysData(string jsonResponse) {
        // Assuming JSON response is an array of toy types for the pet
        List<string> toys = new List<string>();

        // Assuming the API returns a JSON array of toy types
        // Example response: ["toy_1", "toy_3"]
        var toyData = JsonUtility.FromJson<ToyList>(jsonResponse);  // Deserialize JSON response
        toys.AddRange(toyData.toys);  // Add all toy types to the list

        return toys;
    }

    // Method to update the UI based on the available toys
    private void UpdateUIBasedOnToys(List<string> toys)
    {
        // Loop through all possible toy types (toy_1 to toy_6)
        for (int i = 0; i < 6; i++)
        {
            string toyType = "toy_" + (i + 1); // Generate toy type name (toy_1, toy_2, etc.)

            // If the current toy type exists in the list of toys, activate the corresponding toggle
            if (toys.Contains(toyType))
            {
                Toggles[i].SetActive(true);
                PlayerPrefs.GetInt(PlayerPrefKeys.toyPrefix + i, 1);
                // Deactivate the corresponding price tag if necessary (you can customize this logic)
                if (i < PriceTag.Length)
                {
                    PriceTag[i-1].SetActive(false);  // Assuming the price tag should be hidden when the toy exists
                }
            }
            else
            {
                // Deactivate the toggle if the toy doesn't exist
                Toggles[i].SetActive(false);
            }
        }
        PlayerPrefs.Save();
    }

    public void showConfirm(int selected)
    {
        // Set the static selectedSoapIndex for backend and UI updates
        selectedToyIndex = selected;

        // Update the confirmation panel text with soap name and cost
        Confirmtext.text = $"Are you sure you want to buy {toyname[selected]} for {prices[selected]} Coins?";

        // Show the confirmation panel
        ConfirmPanel.SetActive(true);
    }

    public void OnConfirm()
    {
        ReduceCoinsAndUpdateToy();
    }

    private void ReduceCoinsAndUpdateToy()
    {
        string petKey = PlayerPrefKeys.PetPrefix;
        int currentCoins = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetCoins);
        int toyCost = prices[selectedToyIndex]; // Fetch the cost for the selected soap
        string toyTypes = toy_types[selectedToyIndex]; // Get the correct soap type string

        if (currentCoins >= toyCost)
        {
            SM.PlayCoinSound();
            // Deduct the coins
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetCoins, currentCoins - toyCost);

            // Save the soap transaction
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.soap_quantity, 1); // Assuming 1 soap is bought

            // Sync with the backend
            string url = $"{apiUrlSecondary}/pets/{petId}/soap"; // Use the secondary API
            string jsonData = JsonUtility.ToJson(new { toy_type = toyTypes });

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
                Debug.Log($"Successfully purchased {response.toy_type} for {response.new_coins} coins");

                // Optionally, you can update the player's toy UI or handle the UI transition here
                // For example, update the toggle and price tag display based on new toy data
                UpdateUIBasedOnToys(new List<string> { response.toy_type });
            }
        }
        else
        {
            // Show error message if the request failed
            Debug.LogError("Error purchasing toy: " + request.error);
            ShowNotEnoughCoinsPanel();
        }
    }

    private void ShowNotEnoughCoinsPanel()
    {
        notEnoughCoinsPanel.SetActive(true);
    }
    
    public void TogglingToys(int index)
    {
        bool status = toggleStatus[index];
        PlayerPrefs.SetInt(PlayerPrefKeys.toyPrefix + index, status ? 1 : 0);
        Toys[index].SetActive(status);
        PlayerPrefs.Save();
    }
}

// Class to deserialize the JSON response (example structure)
[System.Serializable]
public class ToyList
{
    public List<string> toys;  // List of toy types for the pet
}

// Response structure for the toy purchase response
[System.Serializable]
public class ToyPurchaseResponse
{
    public string toy_type;
    public int new_coins;
}