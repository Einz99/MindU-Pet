using System.Collections;
using UnityEngine;

public class StatManager : MonoBehaviour
{
    [Header("Button Fill References")]
    private int[] statFill = new int[4];

    public bool isSleeping = false;

    public HorizontalPageScroller HPS;

    void Start()
    {
        try
        {
            Debug.Log("📊 StatManager.Start() - Beginning");

            // Find the HPS if not assigned in Inspector
            if (HPS == null)
            {
                HPS = FindObjectOfType<HorizontalPageScroller>();
                
                if (HPS == null)
                {
                    Debug.LogError("❌ HorizontalPageScroller not found!");
                }
                else
                {
                    Debug.Log("✅ HorizontalPageScroller found via FindObjectOfType");
                }
            }

            // Start the stats update coroutine with delay
            StartCoroutine(WaitAndStartStatsUpdate());
            
            Debug.Log("✅ StatManager.Start() - Completed");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Exception in StatManager.Start(): {ex.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}");
        }
    }

    // ✅ NEW: Wait for initialization before starting stats updates
    private IEnumerator WaitAndStartStatsUpdate()
    {
        Debug.Log("⏳ Waiting for HPS initialization before starting stat updates...");

        // Wait for HPS to exist and be ready
        float timeout = 10f;
        float elapsed = 0f;

        while (HPS == null && elapsed < timeout)
        {
            HPS = FindObjectOfType<HorizontalPageScroller>();
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }

        if (HPS == null)
        {
            Debug.LogError("❌ HPS not found after timeout! Stats will not update.");
            yield break;
        }

        // Wait additional time for HPS to fully initialize
        yield return new WaitForSeconds(1f);

        Debug.Log("✅ Starting stats update coroutine");
        StartCoroutine(UpdateStatsEvery10Minutes());
    }

    // Coroutine to update stats every 10 minutes
    private IEnumerator UpdateStatsEvery10Minutes()
    {
        // Wait until the next 10-minute interval
        yield return new WaitForSeconds(CalculateTimeUntilNextInterval());

        while (true)
        {
            try
            {
                string petKey = PlayerPrefKeys.PetPrefix;

                // Retrieve stats from PlayerPrefs
                statFill[0] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetPlayfulness) - 10;
                statFill[1] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHunger) - 5;
                statFill[2] = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHygiene) - 3;

                bool isSleepingNow = PlayerPrefs.GetInt(PlayerPrefKeys.isSleeping) == 1;
                if (isSleepingNow)
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

                // ✅ Save clamped values using StorageBridge
                if (StorageBridge.Instance != null)
                {
                    StorageBridge.Instance.SaveValue(petKey + PlayerPrefKeys.PetPlayfulness, statFill[0]);
                    StorageBridge.Instance.SaveValue(petKey + PlayerPrefKeys.PetHunger, statFill[1]);
                    StorageBridge.Instance.SaveValue(petKey + PlayerPrefKeys.PetHygiene, statFill[2]);
                    StorageBridge.Instance.SaveValue(petKey + PlayerPrefKeys.PetSleep, statFill[3]);
                }
                else
                {
                    Debug.LogWarning("⚠️ StorageBridge.Instance is null, using PlayerPrefs directly");
                    PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetPlayfulness, statFill[0]);
                    PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetHunger, statFill[1]);
                    PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetHygiene, statFill[2]);
                    PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetSleep, statFill[3]);
                    PlayerPrefs.Save();
                }

                // ✅ Add null check before calling
                if (HPS != null)
                {
                    HPS.UpdateButtonFill(statFill);
                    Debug.Log($"📊 Stats updated: Play={statFill[0]}, Hunger={statFill[1]}, Hygiene={statFill[2]}, Sleep={statFill[3]}");
                }
                else
                {
                    Debug.LogError("❌ HPS is null when trying to update stats!");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Exception in UpdateStatsEvery10Minutes(): {ex.Message}");
                Debug.LogError($"Stack trace: {ex.StackTrace}");
            }

            yield return new WaitForSeconds(600f); // 10 minutes
        }
    }

    // This method calculates how long until the next 10-minute interval (e.g., 6:10, 6:20)
    private float CalculateTimeUntilNextInterval()
    {
        try
        {
            // Get the current time
            System.DateTime currentTime = System.DateTime.Now;

            // Calculate the minutes portion of the current time
            int minutes = currentTime.Minute;

            // Find the number of seconds until the next 10-minute interval
            int nextInterval = ((minutes / 10) + 1) * 10; // Find the next multiple of 10 (e.g., 6:10, 6:20, etc.)
            int secondsUntilNextInterval = (nextInterval - minutes) * 60 - currentTime.Second;

            Debug.Log($"⏰ Next stat update in {secondsUntilNextInterval} seconds");

            // Return the seconds until the next 10-minute mark
            return secondsUntilNextInterval;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Exception in CalculateTimeUntilNextInterval(): {ex.Message}");
            return 600f; // Default to 10 minutes if calculation fails
        }
    }
}