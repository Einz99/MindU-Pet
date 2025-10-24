using System;
using Unity.VisualScripting;
using UnityEngine;

public class UnityDataReceiver : MonoBehaviour
{
    // This method will receive data from React Native and store it using DataManager

    void Start()
    {
        UnityDataReceiver receiver = FindFirstObjectByType<UnityDataReceiver>(); // Assuming only one instance of UnityDataReceiver in the scene
        if (receiver != null)
        {
            // Simulate receiving data from React Native
            receiver.ReceiveDataFromReactNative("622,http://10.186.218.142:3000");
        }
    }
    public void ReceiveDataFromReactNative(string data)
    {
        // Split the received data (student_id, API_URL, API_URL_Secondary) by the comma
        string[] dataArray = data.Split(',');

        // Check if we have the correct number of parameters
        if (dataArray.Length == 2)
        {
            int studentId = int.Parse(dataArray[0]);  // First value: student_id
            string apiUrl = dataArray[1];     // Second value: primary API_URL
            // Use DataManager to save the received data
            DataManager.Instance.SetData(studentId, apiUrl);  // Save all three data points
        }
        else
        {
            Debug.LogError("Received data is in an incorrect format. Expected 2 values.");
        }
    }
}
