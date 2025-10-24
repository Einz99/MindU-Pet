using System.Collections;
using UnityEngine;

public class StatManager : MonoBehaviour
{
    [Header("Button Fill References")]
    private int[] statFill = new int[4];

    public bool isSleeping = false;

    public HorizontalPageScroller HPS;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
            string petKey = PlayerPrefKeys.PetPrefix;
            // Retrieve stats from PlayerPrefs
            statFill[0] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetPlayfulness); // playfulness
            statFill[1] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHunger) - 5;      // hunger
            statFill[2] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHygiene) - 3;     // hygiene
            bool isSleeping = PlayerPrefs.GetInt(PlayerPrefKeys.isSleeping) == 1;
            if (isSleeping)
            {
                statFill[3] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetSleep) + 7; 
            } else
            {
                statFill[3] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetSleep) - 7;
            }

            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetPlayfulness, statFill[0]);
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetHunger, statFill[1]);
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetHygiene, statFill[2]);
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetSleep, statFill[3]);
            PlayerPrefs.Save();
            HPS.UpdateButtonFill(statFill);

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
}
