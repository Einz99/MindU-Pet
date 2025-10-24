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
    public SoundManager SM;

    // Coin deduction values for each soap (buying adds 3 quantity)
    private int[] soapCosts = new int[] { 5, 10, 15, 20 };

    // Soap type strings corresponding to the index
    private string[] soapTypes = new string[] { "soap_1", "soap_2", "soap_3", "soap_4" };

    // Soap names for display
    private string[] soapNames = new string[] { "Fresh Puppy", "Tutti Frutie", "Summer Air", "Zen Garden" };

    // Currently active soap index
    private int currentActiveSoapIndex = 0;

    // Static variable to store the selected soap index for purchase
    private int selectedSoapIndex;

    private bool isToggling;

    // Dictionary to store soap quantities fetched from backend
    private System.Collections.Generic.Dictionary<string, int> soapQuantities = 
        new System.Collections.Generic.Dictionary<string, int>();

    void Start()
    {
        string petKey = PlayerPrefKeys.PetPrefix;
        // Retrieve the API URL and pet ID from PlayerPrefs
        apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL, "http://192.168.1.5:3000");
        apiUrlSecondary = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary, apiUrl + "/api");
        petId = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);

        // Fetch all soap data from backend
        StartCoroutine(FetchAllSoapData());
    }

    // Fetch all soap types and quantities from backend on start
    private IEnumerator FetchAllSoapData()
    {
        string url = $"{apiUrlSecondary}/pets/{petId}/soap";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            Debug.Log("Soap data response: " + responseText);

            // Parse the response - it's an array of soap objects
            SoapResponse[] soapData = JsonUtility.FromJson<SoapResponseList>("{\"soapResponses\":" + responseText + "}").soapResponses;

            // Initialize all soap quantities to 0
            for (int i = 0; i < soapTypes.Length; i++)
            {
                soapQuantities[soapTypes[i]] = 0;
            }

            // Update quantities from backend response
            if (soapData != null && soapData.Length > 0)
            {
                foreach (var soap in soapData)
                {
                    if (!string.IsNullOrEmpty(soap.soap_type))
                    {
                        soapQuantities[soap.soap_type] = soap.quantity;
                    }
                }
            }

            // Set the initial active soap (default to soap_1 or first available)
            int initialSoapIndex = 0;
            if (PlayerPrefs.HasKey(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.soap_type))
            {
                string savedSoapType = PlayerPrefs.GetString(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.soap_type);
                initialSoapIndex = System.Array.IndexOf(soapTypes, savedSoapType);
                if (initialSoapIndex < 0) initialSoapIndex = 0;
            }

            OnToggle(initialSoapIndex);
        }
        else
        {
            Debug.LogError("Error fetching soap data: " + request.error);
            // Default to soap_1 with 0 quantity if fetch fails
            for (int i = 0; i < soapTypes.Length; i++)
            {
                soapQuantities[soapTypes[i]] = 0;
            }
            OnToggle(0);
        }
    }

    public void OnToggle(int soapIndex)
    {
        if (isToggling) return;
        isToggling = true;

        // Prevent toggling off - always keep one active
        currentActiveSoapIndex = soapIndex;

        // Update all toggles - only the selected one should be on
        for (int i = 0; i < toggles.Length; i++)
        {
            if (toggles[i] != null)
            {
                toggles[i].isOn = (i == soapIndex);
            }
        }

        // Update the soap sprite
        soap.sprite = soaps[soapIndex];

        // Get the quantity for this soap type
        int quantity = 0;
        if (soapQuantities.ContainsKey(soapTypes[soapIndex]))
        {
            quantity = soapQuantities[soapTypes[soapIndex]];
        }

        // Update the quantity display
        soapquantity.text = $"{quantity}x";

        // Save the current soap type to PlayerPrefs
        PlayerPrefs.SetString(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.soap_type, soapTypes[soapIndex]);
        PlayerPrefs.SetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.soap_quantity, quantity);
        PlayerPrefs.Save();

        // Update backend to set this soap as active (is_in_use = TRUE)
        StartCoroutine(UpdateActiveSoapOnServer(soapTypes[soapIndex]));

        isToggling = false;
    }

    // Update which soap is currently active on the backend
    private IEnumerator UpdateActiveSoapOnServer(string soapType)
    {
        string url = $"{apiUrlSecondary}/pets/{petId}/soap/active";
        string jsonData = JsonUtility.ToJson(new soapPayload { soap_type = soapType });

        UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT)
        {
            uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Active soap updated successfully: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Error updating active soap: " + request.error);
        }
    }

    public void showConfirm(int selected)
    {
        // Set the static selectedSoapIndex for backend and UI updates
        selectedSoapIndex = selected;

        // Update the confirmation panel text with soap name and cost
        Confirmtext.text = $"Are you sure you want to buy {soapNames[selected]} for {soapCosts[selected]} Coins? (+3 uses)";

        // Show the confirmation panel
        ConfirmPanel.SetActive(true);
    }

    public void OnConfirm()
    {
        if(gameObject.activeInHierarchy)
        {
            ReduceCoinsAndUpdateSoap();
        }
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

            // Sync with the backend (this adds 3 to quantity)
            string url = $"{apiUrlSecondary}/pets/{petId}/soap";
            string jsonData = JsonUtility.ToJson(new soapPayload { soap_type = soapType });

            Debug.Log("Sending JSON data: " + jsonData);

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

            // Parse the response to get the new quantity
            var response = JsonUtility.FromJson<SoapPurchaseResponse>(request.downloadHandler.text);
            
            // Update local quantity
            if (response != null)
            {
                soapQuantities[soapTypes[selectedSoapIndex]] = response.new_quantity;
                
                // Update PlayerPrefs
                PlayerPrefs.SetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.soap_quantity, response.new_quantity);
            }

            PlayerPrefs.Save();

            // Update the displayed coin amount
            string petKey2 = PlayerPrefKeys.PetPrefix;
            foreach (var coin in Coins)
            {
                coin.text = PlayerPrefs.GetInt(petKey2 + PlayerPrefKeys.PetCoins).ToString();
            }

            // Automatically toggle to the bought soap
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
        ConfirmPanel.SetActive(false);
    }

    private void ShowNotEnoughCoinsPanel()
    {
        notEnoughCoinsPanel.SetActive(true);
    }

    // Add listeners to prevent toggles from being turned off
    public void OnToggleValueChanged(int index)
    {
        // If someone tries to turn off the current toggle, turn it back on
        if (!toggles[index].isOn && index == currentActiveSoapIndex)
        {
            toggles[index].isOn = true;
        }
        // If someone turns on a different toggle, switch to it
        else if (toggles[index].isOn && index != currentActiveSoapIndex)
        {
            OnToggle(index);
        }
    }
}

[System.Serializable]
public class SoapResponse
{
    public string soap_type;
    public int quantity;
}

[System.Serializable]
public class soapPayload
{
    public string soap_type;
}

[System.Serializable]
public class SoapResponseList
{
    public SoapResponse[] soapResponses;
}

[System.Serializable]
public class SoapPurchaseResponse
{
    public int pet_id;
    public string soap_type;
    public int new_quantity;
    public int new_coins;
    public bool is_in_use;
}