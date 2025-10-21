using System.Collections;
using UnityEngine;

public class StatManager : MonoBehaviour
{
    [Header("Button Fill References")]
    public ButtonFill[] Stats; // Array of ButtonFill references for each stat (hunger, playfulness, hygiene, sleep)

    // Local int array to store the stats values (playfulness, hunger, hygiene, sleep)
    private int[] statFill = new int[4]; // Index 0: playfulness, 1: hunger, 2: hygiene, 3: sleep

    public bool isSleeping = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get the pet stats directly from PlayerPrefs (no need for Pet class)
        string petKey = PlayerPrefKeys.PetPrefix; // Unique pet key, assuming only one pet
        
        // Retrieve stats from PlayerPrefs
        statFill[0] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetPlayfulness); // playfulness
        statFill[1] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHunger);      // hunger
        statFill[2] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHygiene);     // hygiene
        statFill[3] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetSleep);       // sleep

        // Update the stats UI based on the retrieved values
        UpdateStatsUI();

        // Start the stats update coroutine
        StartCoroutine(UpdateStatsEvery10Minutes());
    }

    // Coroutine to update stats every 10 minutes
    private IEnumerator UpdateStatsEvery10Minutes()
    {
        // Wait until the next 10-minute mark
        yield return new WaitForSeconds(CalculateTimeUntilNextInterval());

        // Now that we're synchronized, start the regular 10-minute updates
        while (true)
        {
            UpdateStats(); // Perform the stat update

            // Wait for 10 minutes before updating again
            yield return new WaitForSeconds(600f); // 600 seconds = 10 minutes
        }
    }

    // This method calculates how long until the next 10-minute interval (e.g., 6:10, 6:20)
    private float CalculateTimeUntilNextInterval()
    {
        // Get the current time
        System.DateTime currentTime = System.DateTime.Now;

        // Calculate the minutes portion of the current time
        int minutes = currentTime.Minute;

        // Find the number of seconds until the next 10-minute interval
        int nextInterval = ((minutes / 10) + 1) * 10; // Find the next multiple of 10 (e.g., 6:10, 6:20, etc.)
        int secondsUntilNextInterval = (nextInterval - minutes) * 60 - currentTime.Second;

        // Return the seconds until the next 10-minute mark
        return secondsUntilNextInterval;
    }

    // This method will update the stats (hunger, hygiene, and sleep)
    private void UpdateStats()
    {
        // Update hunger (decrease by 5)
        statFill[1] = Mathf.Max(0, statFill[1] - 5); // Clamp to ensure it doesn't go below 0

        // Update hygiene (decrease by 3)
        statFill[2] = Mathf.Max(0, statFill[2] - 3); // Clamp to ensure it doesn't go below 0

        // Update sleep
        if (isSleeping)
        {
            // If the pet is sleeping, increase sleep by 3
            statFill[3] = Mathf.Min(100, statFill[3] + 3); // Clamp to ensure it doesn't go above 100
        }
        else
        {
            // If the pet is not sleeping, decrease sleep by 5
            statFill[3] = Mathf.Max(0, statFill[3] - 5); // Clamp to ensure it doesn't go below 0
        }

        // Update the UI
        UpdateStatsUI();
    }

    // Update the ButtonFill UI elements for hunger, playfulness, hygiene, and sleep
    void UpdateStatsUI()
    {
        // Assuming Stats is an array of ButtonFill objects, where each index corresponds to a stat
        for (int i = 0; i < statFill.Length; i++)
        {
            Stats[i].SetFillPercentage(statFill[i]); // Set the fill percentage for each stat
        }
    }
}
