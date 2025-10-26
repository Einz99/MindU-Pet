using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class InitialSoap : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public SpriteRenderer Soap;
    public Sprite[] soaps;
    public TMP_Text soapquantity;
    private string apiUrl;
    private string apiUrlSecondary;
    private int petId;
    private string[] soapTypes = new string[] { "soap_1", "soap_2", "soap_3", "soap_4" };
    void Start()
    {
        string petKey = PlayerPrefKeys.PetPrefix;
        // Retrieve the API URL and pet ID from PlayerPrefs
        apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL);
        apiUrlSecondary = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary);
        petId = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);
        Debug.Log($"From InitialSoap:\nRootAPI: {apiUrl}\nAPI: {apiUrlSecondary}\nPetID: {petId}");

        StartCoroutine(FetchSoapData());
    }

    private IEnumerator FetchSoapData()
    {
        string url = $"{apiUrlSecondary}/pets/{petId}/soap"; // Fetch soap type and quantity from the backend
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;

            // Deserialize the response into an array of SoapResponse
            SoapResponse[] soapResponses = JsonUtility.FromJson<SoapResponseList>("{\"soapResponses\":" + jsonResponse + "}").soapResponses;

            if (soapResponses.Length > 0)
            {
                // Assuming we only need the first soap response
                var response = soapResponses[0];

                // Set soap type and quantity fetched from the backend
                PlayerPrefs.SetString(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.soap_type, response.soap_type);
                PlayerPrefs.SetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.soap_quantity, response.quantity);
                PlayerPrefs.Save();

                // Update the UI with the soap quantity
                soapquantity.text = response.quantity.ToString() + "x";
                Soap.sprite = soaps[PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.soap_type)];
            }
        }
        else
        {
            Debug.LogError("Error fetching soap data: " + request.error);
        }
}
}
