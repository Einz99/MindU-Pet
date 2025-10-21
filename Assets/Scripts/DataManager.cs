using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System;

public class DataManager : MonoBehaviour
{
    // Singleton instance
    public static DataManager Instance { get; private set; }

    public int studentId;
    public string apiUrl; // Primary API URL
    public string apiUrlSecondary; // Secondary API URL (appended with "/api")

    // Define the URL for your pets endpoint
    private string petsEndpoint => apiUrlSecondary + "/pets/" + studentId;

    // Loading Screen UI (you can assign this in the Unity Inspector)
    public GameObject loadingScreen;
    public GameObject CreationScreen;
    public float loadingScreenDuration = 5f;  // Set the duration of loading screen (5-10 seconds)

    private void Awake()
    {
        // Ensure only one instance of DataManager exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // Keep this object between scene loads
        }
        else
        {
            Destroy(gameObject);  // Destroy any duplicate instance of the DataManager
        }

        // On start, load the data from PlayerPrefs
        LoadData();
        // Call the function to get pet data and handle scenes after showing loading screen
        StartCoroutine(HandlePetDataRequest());
    }

    // Method to load data from PlayerPrefs
    private void LoadData()
    {
        // Retrieve student_id and apiUrl from PlayerPrefs, with default values if not found
        studentId = PlayerPrefs.GetInt(PlayerPrefKeys.StudentID, 46);
        apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL, "http://192.168.1.5:3000");  // Fallback URL if not found
        apiUrlSecondary = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary, apiUrl + "/api");  // Fallback secondary API URL

        Debug.Log($"Student ID: {studentId}, Primary API URL: {apiUrl}, Secondary API URL: {apiUrlSecondary}");
    }

    // Coroutine to get pet data and handle the loading screen and scene transitions
    public IEnumerator HandlePetDataRequest()
    {
        // Show loading screen
        loadingScreen.SetActive(true);
        CreationScreen.SetActive(false);
    
        // Wait for the specified loading screen duration (5-10 seconds)
        yield return new WaitForSeconds(loadingScreenDuration);

        UnityWebRequest request = UnityWebRequest.Get(petsEndpoint);
        yield return request.SendWebRequest(); // Wait for the request to finish
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            // Parse the JSON response
            string responseText = request.downloadHandler.text;

            try
            {
                PetsWrapper petsWrapper = JsonUtility.FromJson<PetsWrapper>(responseText); // Parse the JSON into a PetsWrapper object

                // Check if the response was successfully parsed
                if (petsWrapper == null)
                {
                    Debug.LogError("Failed to parse pet data.");
                    yield break;
                }

                if (petsWrapper.shouldGoToAdoption)
                {
                    // No pets found, show the Pet Creation Screen
                    loadingScreen.SetActive(false);
                    CreationScreen.SetActive(true);
                }
                else
                {
                    // Pet data found, save it to PlayerPrefs and go to the pet scene
                    SavePetData(petsWrapper.pet);
                    ShowPetScene();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error parsing the response: " + e.Message);
            }
        }
        else
        {
            // If the status is 404, handle it here
            if (request.responseCode == 404)
            {
                // Show the Pet Creation Screen
                loadingScreen.SetActive(false);
                CreationScreen.SetActive(true);
            }
            else
            {
                Debug.LogError("Error fetching pet data: " + request.error);
            }
        }
    }


    // Show the Pet Scene with the data (load next scene if pet found)
    private void ShowPetScene()
    {
        // Start the async scene loading
        StartCoroutine(LoadSceneAsync("Pet"));
    }
    
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Start the asynchronous scene loading
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);

        // Prevent the scene from activating immediately
        asyncOperation.allowSceneActivation = false;

        // While the scene is still loading, show loading progress or animations
        while (!asyncOperation.isDone)
        {
            // Optionally: Show progress (progress is a value between 0 and 0.9)
            float progress = asyncOperation.progress;
            Debug.Log("Loading progress: " + progress);

            // When the loading reaches 90% or more, allow the scene to activate
            if (progress >= 0.9f)
            {
                // Once loading is almost done, we can trigger the activation
                asyncOperation.allowSceneActivation = true;
            }

            yield return null; // Wait until the next frame
        }
    }

    // Save pet data to PlayerPrefs
    private void SavePetData(Pet pets)
    {
        string petKey = PlayerPrefKeys.PetPrefix; // Unique pet key (e.g., "Pet_0", "Pet_1")

        PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetID, pets.id);
        PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetStudentID, pets.student_id);
        PlayerPrefs.SetString(petKey + PlayerPrefKeys.PetName, pets.pet_name);
        PlayerPrefs.SetString(petKey + PlayerPrefKeys.PetType, pets.pet_type);
        PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetCoins, pets.coins);
        PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetFoodStack, pets.food_stack);
        PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetHead, pets.pet_head);
        PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetNeck, pets.pet_neck);
        PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetEyes, pets.pet_eyes);
        PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetHunger, pets.hunger);
        PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetPlayfulness, pets.playfulness);
        PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetHygiene, pets.hygiene);
        PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetSleep, pets.sleep);
        PlayerPrefs.SetString(petKey + PlayerPrefKeys.PetCreatedAt, pets.created_at);
        PlayerPrefs.SetString(petKey + PlayerPrefKeys.PetUpdatedAt, pets.updated_at);

        if (!PlayerPrefs.HasKey(petKey + PlayerPrefKeys.isCurtainOpen))
        {
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.isCurtainOpen, 0);  // Set default value if key doesn't exist
        }
        if (!PlayerPrefs.HasKey(petKey + PlayerPrefKeys.soap_type))
        {
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.soap_type, 0);  // Set default value if key doesn't exist
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.soap_quantity, 3);
        }
        PlayerPrefs.Save();  // Save all PlayerPrefs data
    }

    // Method to set the data in PlayerPrefs
    public void SetData(int studentId, string apiUrl)
    {
        this.studentId = studentId;
        this.apiUrl = apiUrl;

        // Save to PlayerPrefs
        PlayerPrefs.SetInt(PlayerPrefKeys.StudentID, studentId);  // Save student ID
        PlayerPrefs.SetString(PlayerPrefKeys.API_URL, apiUrl);  // Save primary API URL
        string secondaryAPI = apiUrl;
        if (!secondaryAPI.EndsWith("/"))
        {
            secondaryAPI += "/";
        }
        PlayerPrefs.SetString(PlayerPrefKeys.API_URL_Secondary, secondaryAPI + "api");  // Save secondary API URL
        PlayerPrefs.Save();  // Save data immediately
    }
}

// Class to wrap the array of pets (because JsonUtility doesn't handle arrays directly)
[System.Serializable]
public class PetsWrapper
{
    public Pet pet; // Single pet, not an array
    public bool shouldGoToAdoption; // Flag for whether to go to the adoption scene
}

[System.Serializable]
public class Pet
{
    public int id;
    public int student_id;
    public string pet_name;
    public string pet_type;
    public int coins = 20;
    public int food_stack = 5;
    public int pet_head = 0;
    public int pet_neck = 0;
    public int pet_eyes = 0;
    public int hunger = 70;
    public int playfulness = 70;
    public int hygiene = 70;
    public int sleep = 70;
    public string created_at;
    public string updated_at;
    public int soap_type;
    public int soap_quantity;
}

