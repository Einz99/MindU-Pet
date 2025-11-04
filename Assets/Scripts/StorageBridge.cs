using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class StorageBridge : MonoBehaviour
{
    public static StorageBridge Instance { get; private set; }
    
    private bool isWebGL = false;
    
    // Import JavaScript functions for WebGL
    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SaveToAsyncStorage(string key, string value);
    
    [DllImport("__Internal")]
    private static extern void GetFromAsyncStorage(string key);
    
    [DllImport("__Internal")]
    private static extern void CloseWebView();
    
    [DllImport("__Internal")]
    private static extern void SaveAllPlayerPrefs(string jsonData);
    #endif
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        isWebGL = Application.platform == RuntimePlatform.WebGLPlayer;
        Debug.Log($"StorageBridge initialized. WebGL: {isWebGL}");
    }
    
    // Save a single value
    public void SaveValue(string key, string value)
    {
        if (isWebGL)
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            SaveToAsyncStorage(key, value);
            Debug.Log($"💾 Saved to AsyncStorage: {key} = {value}");
            #endif
        }
        else
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
            Debug.Log($"💾 Saved to PlayerPrefs: {key} = {value}");
        }
    }
    
    public void SaveValue(string key, int value)
    {
        SaveValue(key, value.ToString());
    }
    
    public void SaveValue(string key, float value)
    {
        SaveValue(key, value.ToString());
    }
    
    // Load a single value (WebGL will call back via ReceiveAsyncStorageValue)
    public void LoadValue(string key, Action<string> callback)
    {
        if (isWebGL)
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            // Store callback for when JS returns the value
            pendingCallbacks[key] = callback;
            GetFromAsyncStorage(key);
            Debug.Log($"📥 Requesting from AsyncStorage: {key}");
            #endif
        }
        else
        {
            string value = PlayerPrefs.GetString(key, "");
            callback?.Invoke(value);
            Debug.Log($"📥 Loaded from PlayerPrefs: {key} = {value}");
        }
    }
    
    private Dictionary<string, Action<string>> pendingCallbacks = new Dictionary<string, Action<string>>();
    
    // Called by JavaScript when AsyncStorage returns a value
    public void ReceiveAsyncStorageValue(string data)
    {
        try
        {
            // Expected format: "key:value"
            string[] parts = data.Split(new[] { ':' }, 2);
            if (parts.Length == 2)
            {
                string key = parts[0];
                string value = parts[1];
                
                Debug.Log($"✅ Received from AsyncStorage: {key} = {value}");
                
                if (pendingCallbacks.ContainsKey(key))
                {
                    pendingCallbacks[key]?.Invoke(value);
                    pendingCallbacks.Remove(key);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing AsyncStorage response: {e.Message}");
        }
    }
    
    // Save all critical PlayerPrefs data at once
    public void SaveAllData()
    {
        if (!isWebGL)
        {
            PlayerPrefs.Save();
            Debug.Log("💾 PlayerPrefs saved");
            return;
        }
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            // Collect all important data
            var data = new Dictionary<string, string>();
            
            // Pet data
            string petKey = PlayerPrefKeys.PetPrefix;
            data[petKey + PlayerPrefKeys.PetID] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID, 0).ToString();
            data[petKey + PlayerPrefKeys.PetStudentID] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetStudentID, 0).ToString();
            data[petKey + PlayerPrefKeys.PetName] = PlayerPrefs.GetString(petKey + PlayerPrefKeys.PetName, "");
            data[petKey + PlayerPrefKeys.PetType] = PlayerPrefs.GetString(petKey + PlayerPrefKeys.PetType, "");
            data[petKey + PlayerPrefKeys.PetCoins] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetCoins, 0).ToString();
            data[petKey + PlayerPrefKeys.PetFoodStack] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetFoodStack, 0).ToString();
            data[petKey + PlayerPrefKeys.PetHead] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHead, 0).ToString();
            data[petKey + PlayerPrefKeys.PetNeck] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetNeck, 0).ToString();
            data[petKey + PlayerPrefKeys.PetEyes] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetEyes, 0).ToString();
            data[petKey + PlayerPrefKeys.PetHunger] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHunger, 70).ToString();
            data[petKey + PlayerPrefKeys.PetPlayfulness] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetPlayfulness, 70).ToString();
            data[petKey + PlayerPrefKeys.PetHygiene] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHygiene, 70).ToString();
            data[petKey + PlayerPrefKeys.PetSleep] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetSleep, 70).ToString();
            data[petKey + PlayerPrefKeys.PetCreatedAt] = PlayerPrefs.GetString(petKey + PlayerPrefKeys.PetCreatedAt, "");
            data[petKey + PlayerPrefKeys.PetUpdatedAt] = PlayerPrefs.GetString(petKey + PlayerPrefKeys.PetUpdatedAt, "");
            
            // Additional keys
            data[petKey + PlayerPrefKeys.isCurtainOpen] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.isCurtainOpen, 0).ToString();
            data[petKey + PlayerPrefKeys.soap_type] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.soap_type, 0).ToString();
            data[petKey + PlayerPrefKeys.soap_quantity] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.soap_quantity, 3).ToString();
            data[PlayerPrefKeys.isSleeping] = PlayerPrefs.GetInt(PlayerPrefKeys.isSleeping, 0).ToString();
            
            // Toys (save up to 10 toy slots)
            for (int i = 0; i < 10; i++)
            {
                string toyKey = PlayerPrefKeys.toyPrefix + i;
                if (PlayerPrefs.HasKey(toyKey))
                {
                    data[toyKey] = PlayerPrefs.GetInt(toyKey, 0).ToString();
                }
            }
            
            // Daily rewards
            if (PlayerPrefs.HasKey("LoginStreak"))
                data["LoginStreak"] = PlayerPrefs.GetInt("LoginStreak", 0).ToString();
            if (PlayerPrefs.HasKey("LastLoginDate"))
                data["LastLoginDate"] = PlayerPrefs.GetString("LastLoginDate", "");
            if (PlayerPrefs.HasKey("LastDailyReward"))
                data["LastDailyReward"] = PlayerPrefs.GetString("LastDailyReward", "");
            
            if (PlayerPrefs.HasKey("MuteMusic"))
                data["MuteMusic"] = PlayerPrefs.GetInt("MuteMusic", 0).ToString();
            if (PlayerPrefs.HasKey("MuteSound"))
                data["MuteSound"] = PlayerPrefs.GetInt("MuteSound", 0).ToString();
            if (PlayerPrefs.HasKey("MasterVolume"))
                data["MasterVolume"] = PlayerPrefs.GetFloat("MasterVolume", 1f).ToString();
            
            // Convert to JSON
            string json = JsonUtility.ToJson(new SerializableDictionary { data = data });
            
            Debug.Log($"💾 Saving all data to AsyncStorage: {json}");
            SaveAllPlayerPrefs(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving all data: {e.Message}");
        }
        #endif
    }
    
    // Load all data from AsyncStorage on startup
    public void LoadAllData(Action onComplete)
    {
        if (!isWebGL)
        {
            onComplete?.Invoke();
            return;
        }
        
        StartCoroutine(LoadAllDataCoroutine(onComplete));
    }
    
    private IEnumerator LoadAllDataCoroutine(Action onComplete)
    {
        // In WebGL, we need to request data from JS
        // This is called from DataManager after it receives student ID
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("📥 Loading data from AsyncStorage...");
        // The data will be loaded via ReceiveAllPlayerPrefs callback
        onComplete?.Invoke();
    }
    
    // Called by JavaScript with all saved data
    public void ReceiveAllPlayerPrefs(string jsonData)
    {
        try
        {
            Debug.Log($"✅ Received all data from AsyncStorage: {jsonData}");
            
            var wrapper = JsonUtility.FromJson<SerializableDictionary>(jsonData);
            
            if (wrapper != null && wrapper.data != null)
            {
                foreach (var kvp in wrapper.data)
                {
                    // Try to parse as int first
                    if (int.TryParse(kvp.Value, out int intValue))
                    {
                        PlayerPrefs.SetInt(kvp.Key, intValue);
                    }
                    else if (float.TryParse(kvp.Value, out float floatValue))
                    {
                        PlayerPrefs.SetFloat(kvp.Key, floatValue);
                    }
                    else
                    {
                        PlayerPrefs.SetString(kvp.Key, kvp.Value);
                    }
                }
                
                PlayerPrefs.Save();
                Debug.Log("✅ All data loaded into PlayerPrefs");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading all data: {e.Message}");
        }
    }
    
    // Exit and save
    public void ExitGame()
    {
        Debug.Log("🚪 Exiting game and saving data...");
        
        // Save all data before closing
        SaveAllData();
        
        if (isWebGL)
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            // Wait a moment for save to complete, then close
            StartCoroutine(ExitAfterDelay());
            #endif
        }
        else
        {
            // For standalone builds
            Application.Quit();
        }
    }
    
    private IEnumerator ExitAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        CloseWebView();
        Debug.Log("🚪 WebView close signal sent");
        #endif
    }
}

// Helper class for JSON serialization
[Serializable]
public class SerializableDictionary
{
    public Dictionary<string, string> data;
}