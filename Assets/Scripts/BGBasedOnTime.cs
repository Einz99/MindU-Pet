using UnityEngine;
using System;

public class BGBasedOnTime : MonoBehaviour
{
    public Animator FirstBG;
    public RuntimeAnimatorController[] BGControllers;
    public Animator OutsideBG;
    public RuntimeAnimatorController[] OutsideBGs;

    private TimeSpan lastCheckedTime = TimeSpan.Zero; // Store the last checked time
    private int currentBGIndex = -1; // Track the current background index

    private float timeCheckInterval = 120f; // Check time every second
    private float timeSinceLastCheck = 0f;

    void Update()
    {
        timeSinceLastCheck += Time.deltaTime;

        if (timeSinceLastCheck >= timeCheckInterval)
        {
            // Update time check and reset timer
            SetBackgroundBasedOnTime();
            timeSinceLastCheck = 0f;
        }
    }

    void SetBackgroundBasedOnTime()
    {
        DateTime currentTime = DateTime.Now;

        // Skip the time check if the time has not changed significantly
        if (currentTime.TimeOfDay == lastCheckedTime)
            return;

        // Define time ranges
        TimeSpan eveningStart = new TimeSpan(19, 0, 0); // 7 PM
        TimeSpan morningStart = new TimeSpan(5, 0, 0);  // 5 AM
        TimeSpan afternoonStart = new TimeSpan(15, 0, 0); // 3 PM
        TimeSpan eveningEnd = new TimeSpan(19, 0, 0);   // 7 PM

        // Determine the current time range
        int newBGIndex = -1;
        if (currentTime.TimeOfDay >= eveningStart || currentTime.TimeOfDay < morningStart)
        {
            newBGIndex = 0; // 7 PM - 5 AM
        }
        else if (currentTime.TimeOfDay >= morningStart && currentTime.TimeOfDay < afternoonStart)
        {
            newBGIndex = 1; // 5 AM - 3 PM
        }
        else if (currentTime.TimeOfDay >= afternoonStart && currentTime.TimeOfDay < eveningEnd)
        {
            newBGIndex = 2; // 3 PM - 7 PM
        }

        // Only update if the time range has changed
        if (newBGIndex != currentBGIndex)
        {
            // Update the background
            FirstBG.runtimeAnimatorController = BGControllers[newBGIndex];
            OutsideBG.runtimeAnimatorController = OutsideBGs[newBGIndex];
            currentBGIndex = newBGIndex; // Store the new background state
        }

        // Update the last checked time to avoid redundant checks
        lastCheckedTime = currentTime.TimeOfDay;
    }
}
