using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;

public class UnityDataReceiver : MonoBehaviour
{
    [Header("Fallback Configuration")]
    private float timeout = 10f; // Wait 5 seconds for React Native data
    private int fallbackStudentId = 98;
    
    [Header("PC Server IP (for Android builds)")]
    [SerializeField] 
    private string pcServerIP = "https://www.mind-u.space"; // Set your PC's IP here
    private bool dataReceived = false;
    private string fallbackData;
    
    // Import JavaScript functions for WebGL (only used in WebGL builds)
    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string GetURLParameter(string param);
    
    [DllImport("__Internal")]
    private static extern string GetOriginURL();
    #endif
    
    void Start()
    {
        Debug.Log($"📱 Platform: {Application.platform}");
        
        // Check if running in WebGL
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Debug.Log("🌐 WebGL platform detected - using URL parameters");
            StartCoroutine(WaitAndHandleDataWebGL());
        }
        else
        {
            // Original code for Android/PC builds
            string serverIP = GetServerIP();
            fallbackData = $"{fallbackStudentId},https://www.mind-u.space";
            Debug.Log($"🌐 Fallback data generated: {fallbackData}");
            
            // Wait for DataManager to be ready, then handle data
            StartCoroutine(WaitAndHandleData());
        }
    }
    
    private string GetServerIP()
    {
        // If running on Android, use the PC server IP
        if (Application.platform == RuntimePlatform.Android)
        {
            Debug.Log($"📱 Android platform detected, using PC server IP: {pcServerIP}");
            return pcServerIP;
        }
        
        // If running on PC (Editor or Standalone), use local IP
        string localIP = GetLocalIPAddress();
        Debug.Log($"💻 PC platform detected, using local IP: {localIP}");
        return localIP;
    }
    
    private string GetLocalIPAddress()
    {
        try
        {
            // Get all network interfaces
            var host = Dns.GetHostEntry(Dns.GetHostName());
            
            // Find the first IPv4 address that is not loopback
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    Debug.Log($"📍 Found local IPv4: {ip}");
                    return ip.ToString();
                }
            }
            
            Debug.LogWarning("⚠️ No IPv4 address found, using localhost");
            return "127.0.0.1";
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error getting local IP: {ex.Message}");
            return "127.0.0.1";
        }
    }
    
    // Original coroutine for Android/PC builds
    private IEnumerator WaitAndHandleData()
    {
        float elapsed = 0f;
        
        // Wait until DataManager instance exists
        while (DataManager.Instance == null)
        {
            yield return null;
            elapsed += Time.deltaTime;
            
            if (elapsed >= 1f)
            {
                Debug.LogWarning("⏳ Still waiting for DataManager...");
                elapsed = 0f;
            }
        }
        
        Debug.Log("✅ DataManager found!");
        
        // Wait for timeout period to see if React Native sends data
        float waitTime = 0f;
        while (waitTime < timeout && !dataReceived)
        {
            yield return null;
            waitTime += Time.deltaTime;
        }
        
        // If no data received from React Native, use fallback
        if (!dataReceived)
        {
            Debug.LogWarning($"⏰ Timeout reached ({timeout}s). Using fallback data.");
            ReceiveDataFromReactNative(fallbackData);
        }
        else
        {
            Debug.Log("✅ Data successfully received from React Native!");
        }
    }
    
    // New coroutine for WebGL builds
    private IEnumerator WaitAndHandleDataWebGL()
    {
        float elapsed = 0f;
        
        // Wait until DataManager instance exists
        while (DataManager.Instance == null)
        {
            yield return null;
            elapsed += Time.deltaTime;
            
            if (elapsed >= 1f)
            {
                Debug.LogWarning("⏳ Still waiting for DataManager...");
                elapsed = 0f;
            }
        }
        
        Debug.Log("✅ DataManager found!");
        
        // Try to get student ID and API URL from URL
        int studentId = fallbackStudentId;
        string apiUrl = $"https://www.mind-u.space"; // Default fallback
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            // Get the origin (protocol + host + port) where Unity is hosted
            string origin = GetOriginURL();
            
            if (!string.IsNullOrEmpty(origin))
            {
                apiUrl = origin;
                Debug.Log($"✅ API URL from origin: {apiUrl}");
            }
            
            // Get student ID from URL parameter
            string idFromUrl = GetURLParameter("id");
            
            if (!string.IsNullOrEmpty(idFromUrl))
            {
                studentId = int.Parse(idFromUrl);
                dataReceived = true;
                Debug.Log($"✅ Student ID from URL: {studentId}");
            }
            else
            {
                Debug.LogWarning("⚠️ No 'id' parameter in URL, using fallback");
            }
            
            // Optional: Allow API override via query parameter
            string apiFromUrl = GetURLParameter("api");
            if (!string.IsNullOrEmpty(apiFromUrl))
            {
                apiUrl = apiFromUrl;
                Debug.Log($"✅ API URL overridden from parameter: {apiUrl}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error parsing URL parameters: {ex.Message}");
            Debug.LogWarning("⚠️ Using fallback values");
        }
        #else
        Debug.LogWarning("⚠️ WebGL JS functions not available in Editor, using fallback values");
        #endif
        
        // Set the data in DataManager
        Debug.Log($"🔧 Setting - Student ID: {studentId}, API: {apiUrl}");
        DataManager.Instance.SetData(studentId, apiUrl);
        
        if (dataReceived)
        {
            Debug.Log("✅ Data successfully loaded from URL!");
        }
        else
        {
            Debug.Log("ℹ️ Using fallback data for testing.");
        }
    }
    
    public void ReceiveDataFromReactNative(string data)
    {
        if (dataReceived)
        {
            Debug.LogWarning("⚠️ Data already received, ignoring duplicate call.");
            return;
        }
        
        dataReceived = true;
        Debug.Log($"📥 Received data: {data}");
        
        string[] dataArray = data.Split(',');

        if (dataArray.Length == 2)
        {
            int studentId = int.Parse(dataArray[0].Trim());
            string apiUrl = dataArray[1].Trim();
            
            Debug.Log($"🔧 Parsed - Student ID: {studentId}, API: {apiUrl}");
            
            // Make sure DataManager exists
            if (DataManager.Instance != null)
            {
                DataManager.Instance.SetData(studentId, apiUrl);
            }
            else
            {
                Debug.LogError("❌ DataManager instance not found!");
            }
        }
        else
        {
            Debug.LogError("❌ Received data is in incorrect format. Expected 2 values.");
        }
    }
}