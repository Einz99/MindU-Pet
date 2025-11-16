using UnityEngine;
using System;

public class BGBasedOnTime : MonoBehaviour
{
    public Animator FirstBG;
    public RuntimeAnimatorController[] BGControllers;
    public Animator OutsideBG;
    public RuntimeAnimatorController[] OutsideBGs;

    private int currentBGIndex = -1; // Track the current background index
    private float timeCheckInterval = 60f; // Check time every minute (60 seconds, not 6000!)
    private float timeSinceLastCheck = 0f;

    void Start()
    {
        // Set background immediately on start
        SetBackgroundBasedOnTime();
    }

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
        TimeSpan currentTimeOfDay = currentTime.TimeOfDay;

        // Define time ranges
        TimeSpan nightStart = new TimeSpan(19, 0, 0);    // 7:00 PM
        TimeSpan morningStart = new TimeSpan(5, 0, 0);   // 5:00 AM
        TimeSpan afternoonStart = new TimeSpan(15, 0, 0); // 3:00 PM

        int newBGIndex = -1;

        // Night: 7:00 PM (19:00) to 4:59 AM
        // This covers: 19:00-23:59 and 00:00-04:59
        if (currentTimeOfDay >= nightStart || currentTimeOfDay < morningStart)
        {
            newBGIndex = 0; // Night background
        }
        // Morning: 5:00 AM to 2:59 PM
        else if (currentTimeOfDay >= morningStart && currentTimeOfDay < afternoonStart)
        {
            newBGIndex = 1; // Morning background
        }
        // Afternoon: 3:00 PM to 6:59 PM
        else if (currentTimeOfDay >= afternoonStart && currentTimeOfDay < nightStart)
        {
            newBGIndex = 2; // Afternoon background
        }

        // Only update if the time range has changed
        if (newBGIndex != currentBGIndex && newBGIndex != -1)
        {
            // Validate array indices before accessing
            if (newBGIndex < BGControllers.Length && newBGIndex < OutsideBGs.Length)
            {
                // Update the backgrounds
                FirstBG.runtimeAnimatorController = BGControllers[newBGIndex];
                OutsideBG.runtimeAnimatorController = OutsideBGs[newBGIndex];
                currentBGIndex = newBGIndex; // Store the new background state
            }
        }
    }

    // Optional: Method to manually test different times
    [ContextMenu("Test Current Time")]
    void TestCurrentTime()
    {
        Debug.Log($"Current time: {DateTime.Now:HH:mm:ss}");
        SetBackgroundBasedOnTime();
    }

    // Optional: Method to test specific time
    public void TestSpecificTime(int hour, int minute)
    {
        TimeSpan testTime = new TimeSpan(hour, minute, 0);
        Debug.Log($"Testing time: {hour:D2}:{minute:D2}:00");

        TimeSpan nightStart = new TimeSpan(19, 0, 0);
        TimeSpan morningStart = new TimeSpan(5, 0, 0);
        TimeSpan afternoonStart = new TimeSpan(15, 0, 0);

        if (testTime >= nightStart || testTime < morningStart)
        {
            Debug.Log("Result: Night (Index 0)");
        }
        else if (testTime >= morningStart && testTime < afternoonStart)
        {
            Debug.Log("Result: Morning (Index 1)");
        }
        else if (testTime >= afternoonStart && testTime < nightStart)
        {
            Debug.Log("Result: Afternoon (Index 2)");
        }
    }
}

/* 
TIME RANGES:
- Index 0 (Night):     19:00 (7 PM)  - 04:59 (4:59 AM)
- Index 1 (Morning):   05:00 (5 AM)  - 14:59 (2:59 PM)
- Index 2 (Afternoon): 15:00 (3 PM)  - 18:59 (6:59 PM)

EXAMPLE TIMES:
00:00 (12 AM) → Night (0) ✓
03:00 (3 AM)  → Night (0) ✓
05:00 (5 AM)  → Morning (1) ✓
12:00 (12 PM) → Morning (1) ✓
15:00 (3 PM)  → Afternoon (2) ✓
18:00 (6 PM)  → Afternoon (2) ✓
19:00 (7 PM)  → Night (0) ✓
23:59 (11:59 PM) → Night (0) ✓
*/