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
        // Find the HPS if not assigned in Inspector
        if (HPS == null)
        {
            HPS = FindObjectOfType<HorizontalPageScroller>();
            if (HPS == null)
            {
                Debug.LogError("HorizontalPageScroller not found! Make sure it exists in the scene.");
            }
        }

        // Start the stats update coroutine
        StartCoroutine(UpdateStatsEvery10Minutes());
    }

    // Coroutine to update stats every 10 minutes
    private IEnumerator UpdateStatsEvery10Minutes()
    {
        yield return new WaitForSeconds(CalculateTimeUntilNextInterval());

        while (true)
        {
            string petKey = PlayerPrefKeys.PetPrefix;

            // Retrieve stats from PlayerPrefs
            statFill[0] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetPlayfulness) - 10;
            statFill[1] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHunger) - 5;
            statFill[2] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHygiene) - 3;

            bool isSleeping = PlayerPrefs.GetInt(PlayerPrefKeys.isSleeping) == 1;
            if (isSleeping)
            {
                statFill[3] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetSleep) + 7; 
            }
            else
            {
                statFill[3] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetSleep) - 7;
            }

            // ✅ Clamp all values between 0 and 100
            for (int i = 0; i < statFill.Length; i++)
            {
                statFill[i] = Mathf.Clamp(statFill[i], 0, 100);
            }

            // Save clamped values
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetPlayfulness, statFill[0]);
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetHunger, statFill[1]);
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetHygiene, statFill[2]);
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetSleep, statFill[3]);
            PlayerPrefs.Save();

            // ✅ Add null check before calling
            if (HPS != null)
            {
                Debug.Log($"Updating stats: Playfulness={statFill[0]}, Hunger={statFill[1]}, Hygiene={statFill[2]}, Sleep={statFill[3]}");
                HPS.UpdateButtonFill(statFill);
            }
            else
            {
                Debug.LogError("HPS is null! Cannot update button fills.");
            }

            yield return new WaitForSeconds(600f); // 10 minutes
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
