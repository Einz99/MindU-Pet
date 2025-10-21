using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class BathMenu : MonoBehaviour
{
    private string apiUrl;
    private string apiUrlSecondary;
    private int petId;
    public Toggle[] toggles = new Toggle[4];
    public Sprite[] soaps = new Sprite[4];
    public SpriteRenderer soap;
    public TMP_Text soapquantity;
    public GameObject notEnoughCoinsPanel;
    public GameObject ConfirmPanel;
    public TMP_Text Confirmtext;
    public Button ConfirmTransact;
    public TMP_Text[] Coins;

    // Coin deduction values for each soap
    private int[] soapCosts = new int[] { 10, 20, 30, 40 };

    // Soap type strings corresponding to the index
    private string[] soapTypes = new string[] { "soap_1", "soap_2", "soap_3", "soap_4" };

    // Soap names for display
    private string[] soapNames = new string[] { "Fresh Puppy", "Tutti Frutie", "Summer Air", "Zen Garden" };

    // Static variable to store the selected soap index
    private int selectedSoapIndex;

    public SoundManager SM;

    void Start()
    {
        string petKey = PlayerPrefKeys.PetPrefix;
        // Retrieve the API URL and pet ID from PlayerPrefs
        apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL, "http://192.168.1.5:3000");
        apiUrlSecondary = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary, apiUrl + "/api");
        petId = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);

        if (!PlayerPrefs.HasKey(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.soap_type))
        {
            OnToggle(0);
        }
        else
        {
            int soap_type = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.soap_type);
            OnToggle(soap_type);
        }
    }

    public void OnToggle(int soapIndex)
    {
        // Loop through all toggles and set the appropriate one
        foreach (var toggle in toggles)
        {
            toggle.isOn = false;
        }
        toggles[soapIndex].isOn = true;
        soap.sprite = soaps[soapIndex];

        // Fetch soap type and quantity from the backend when toggling
        StartCoroutine(FetchSoapData(soapTypes[soapIndex]));
    }

    // Coroutine to fetch soap type and quantity from the backend
    private IEnumerator FetchSoapData(string soapType)
    {
        string url = $"{apiUrlSecondary}/pets/{petId}/soap"; // Fetch soap type and quantity from the backend
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Parse the response from the backend
            var response = JsonUtility.FromJson<SoapResponse>(request.downloadHandler.text);

            // If no soap data exists or quantity is 0, set the soap type and quantity accordingly
            if (response.soap_type == null || response.quantity == 0)
            {
                soapquantity.text = "Quantity: 0";
                // If no soap exists, set soap type to selected and quantity to 0
                PlayerPrefs.SetString(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.soap_type, soapType);
                PlayerPrefs.SetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.soap_quantity, 0);
                PlayerPrefs.Save();
            }
            else
            {
                // Set the soap type and quantity fetched from the backend
                PlayerPrefs.SetString(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.soap_type, soapType);
                PlayerPrefs.SetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.soap_quantity, response.quantity);
                PlayerPrefs.Save();

                // Update the UI with the soap quantity
                soapquantity.text = "Quantity: " + response.quantity.ToString();
            }
        }
        else
        {
            Debug.LogError("Error fetching soap data: " + request.error);
        }
    }

    public void showConfirm(int selected)
    {
        // Set the static selectedSoapIndex for backend and UI updates
        selectedSoapIndex = selected;

        // Update the confirmation panel text with soap name and cost
        Confirmtext.text = $"Are you sure you want to buy {soapNames[selected]} for {soapCosts[selected]} Coins?";

        // Show the confirmation panel
        ConfirmPanel.SetActive(true);
    }

    public void OnConfirm()
    {
        ReduceCoinsAndUpdateSoap();
    }

    private void ReduceCoinsAndUpdateSoap()
    {
        string petKey = PlayerPrefKeys.PetPrefix;
        int currentCoins = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetCoins);
        int soapCost = soapCosts[selectedSoapIndex]; // Fetch the cost for the selected soap
        string soapType = soapTypes[selectedSoapIndex]; // Get the correct soap type string

        // Check if the player has enough coins
        if (currentCoins >= soapCost)
        {
            SM.PlayCoinSound();
            // Deduct the coins
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetCoins, currentCoins - soapCost);

            // Save the soap transaction
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.soap_quantity, 1); // Assuming 1 soap is bought

            // Sync with the backend
            string url = $"{apiUrlSecondary}/pets/{petId}/soap"; // Use the secondary API
            string jsonData = JsonUtility.ToJson(new { soapType = soapType });

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
            Debug.Log("Soap updated successfully: " + request.downloadHandler.text);
            PlayerPrefs.Save();

            // Update the displayed coin amount
            string petKey2 = PlayerPrefKeys.PetPrefix;
            foreach (var coin in Coins)
            {
                coin.text = PlayerPrefs.GetInt(petKey2 + PlayerPrefKeys.PetCoins).ToString();
            }

            // Automatically toggle the bought soap
            OnToggle(selectedSoapIndex);
        }
        else
        {
            Debug.LogError("Error updating soap: " + request.error);
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
}

[System.Serializable]
public class SoapResponse
{
    public string soap_type;
    public int quantity;
}