using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class APIClient : MonoBehaviour
{
    private string apiUrl;  // Your Node.js server's /test endpoint

    void Start()
    {
        // Retrieve the API URL from PlayerPrefs and append "/test"
        apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL);

        Debug.Log($"From api_client:\nRootAPI: {apiUrl}");

        string apiTest = apiUrl;
        // Ensure there's no trailing slash before appending "/test"
        if (!apiTest.EndsWith("/"))
        {
            apiTest += "/";
        }
        
        apiTest += "test";  // Now add the /test endpoint
        // Test the API call when the script starts
        StartCoroutine(CallAPI(apiTest));
    }

    // Coroutine to call the /test API and handle the response
    private IEnumerator CallAPI(string apiTest)
    {
        UnityWebRequest request = UnityWebRequest.Get(apiTest);  // Create GET request for /test

        // Send the request and wait for a response
        yield return request.SendWebRequest();

        // Handle the response
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            // Handle error
            Debug.LogError("Error: " + request.error);
        }
        else
        {
            // Handle successful response
            Debug.Log("Received response: " + request.downloadHandler.text);  // "Connected successfully"
        }
    }
}
