using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class UnityDataReceiver : MonoBehaviour
{
    [Header("Fallback Configuration")]
    [SerializeField] private float timeout = 5f; // Wait 5 seconds for React Native data
    [SerializeField] private int fallbackStudentId = 46;
    [SerializeField] private int port = 3000;
    
    [Header("PC Server IP (for Android builds)")]
    [SerializeField] 
    private string pcServerIP = "192.168.1.2"; // Set your PC's IP here
    private string pcServerIP2 = "10.186.218.142";
    
    private bool dataReceived = false;
    private string fallbackData;
    
    void Start()
    {
        // Generate fallback data based on platform
        string serverIP = GetServerIP();
        fallbackData = $"{fallbackStudentId},http://{serverIP}:{port}";
        Debug.Log($"🌐 Fallback data generated: {fallbackData}");
        Debug.Log($"📱 Platform: {Application.platform}");
        
        // Wait for DataManager to be ready, then handle data
        StartCoroutine(WaitAndHandleData());
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
    
    private IEnumerator WaitAndHandleData()
    {
        // Wait until DataManager instance exists
        while (DataManager.Instance == null)
        {
            yield return null;
        }
        
        Debug.Log("📡 UnityDataReceiver: Waiting for React Native data...");
        
        // Wait for timeout period
        float elapsedTime = 0f;
        while (elapsedTime < timeout && !dataReceived)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // If no data received after timeout, use fallback
        if (!dataReceived)
        {
            Debug.LogWarning($"⚠️ No React Native data received after {timeout} seconds. Using fallback data.");
            ReceiveDataFromReactNative(fallbackData);
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