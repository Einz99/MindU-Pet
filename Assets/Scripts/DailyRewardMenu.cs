using TMPro;
using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Collections;

public class DailyRewardMenu : MonoBehaviour
{
    public GameObject dailyRewardPanel;
    public TMP_Text[] coins;
    public UnityEngine.UI.Image rewardImage;
    public Sprite[] RewardImg;
    public TMP_Text foodquantity;

    private const string LAST_LOGIN_DATE_KEY = "LastLoginDate";
    private const string LOGIN_STREAK_KEY = "LoginStreak";
    private const string LAST_DAILY_REWARD_KEY = "LastDailyRewardDate";

    private int currentStreak = 0;
    private int currentCoins = 0;
    private int currentFood = 0;
    
    private string apiUrl;
    private string apiUrlSecondary;
    private int petId;

    void OnEnable()
    {
        // Load API and pet data
        LoadAPIData();

        // Load current pet data
        LoadPetData();

        // Calculate login streak
        CalculateLoginStreak();

        // Change the reward sprite based on streak
        UpdateRewardSprite();

        // Display current rewards
        DisplayRewards();
    }

    private void LoadAPIData()
    {
        string petKey = PlayerPrefKeys.PetPrefix;
        // Retrieve the API URL and pet ID from PlayerPrefs
        apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL);
        apiUrlSecondary = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary);
        petId = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);
    }

    private void LoadPetData()
    {
        string petKey = PlayerPrefKeys.PetPrefix;
        currentCoins = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetCoins, 0);
        currentFood = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetFoodStack, 0);
    }
    
    private void CalculateLoginStreak()
    {
        string todayDate = DateTime.Now.ToString("yyyy-MM-dd");
        string lastLoginDate = PlayerPrefs.GetString(LAST_LOGIN_DATE_KEY, "");
        
        if (string.IsNullOrEmpty(lastLoginDate))
        {
            // First time login
            currentStreak = 1;
            Debug.Log("🎉 First login! Day 1");
        }
        else
        {
            DateTime lastLogin = DateTime.Parse(lastLoginDate);
            DateTime today = DateTime.Now.Date;
            
            int daysDifference = (today - lastLogin).Days;
            
            if (daysDifference == 1)
            {
                // Consecutive day login
                currentStreak = PlayerPrefs.GetInt(LOGIN_STREAK_KEY, 1) + 1;
                
                // Reset to day 1 if exceeded 7 days
                if (currentStreak > 7)
                {
                    currentStreak = 1;
                    Debug.Log("🔄 Completed 7 days! Restarting from Day 1");
                }
                else
                {
                    Debug.Log($"✅ Consecutive login! Day {currentStreak}");
                }
            }
            else if (daysDifference == 0)
            {
                // Same day (already logged in today)
                currentStreak = PlayerPrefs.GetInt(LOGIN_STREAK_KEY, 1);
                Debug.Log($"⏭️ Already logged in today. Day {currentStreak}");
            }
            else
            {
                // Streak broken (missed a day)
                currentStreak = 1;
                Debug.Log("💔 Streak broken! Restarting from Day 1");
            }
        }
        
        // Save current streak
        PlayerPrefs.SetInt(LOGIN_STREAK_KEY, currentStreak);
        PlayerPrefs.Save();
    }

    private void UpdateRewardSprite()
    {
        int dayIndex = currentStreak - 1; // Convert to 0-based index (Day 1 = index 0)
        
        // Update reward image based on login streak
        if (rewardImage != null && RewardImg != null && dayIndex >= 0 && dayIndex < RewardImg.Length)
        {
            rewardImage.sprite = RewardImg[dayIndex];
            Debug.Log($"🎁 Displaying reward sprite for Day {currentStreak}");
        }
        else
        {
            Debug.LogWarning($"⚠️ RewardImg array doesn't have sprite for Day {currentStreak}");
        }
    }

    private void DisplayRewards()
    {
        // Coin rewards for days 1-6
        int[] coinRewards = { 5, 10, 15, 20, 25, 30, 0 };

        // Display coins for each day
        if (coins != null && coins.Length > 0)
        {
            for (int i = 0; i < coins.Length && i < 7; i++)
            {
                if (coins[i] != null)
                {
                    int reward = coinRewards[i];

                    if (reward > 0) // Only update if there's a coin reward
                    {
                        coins[i].text = (currentCoins + reward).ToString();
                    }
                    // If reward is 0 (Day 7), just leave the text alone
                }
            }
        }

        // Display food quantity for day 7
        if (foodquantity != null && currentStreak == 7)
        {
            foodquantity.text = (currentFood + 10) + "x";
        }
        else if (foodquantity != null)
        {
            foodquantity.text = currentFood + "x";
        }
    }

    public void ClaimReward()
    {
        // Send claim request to backend
        StartCoroutine(ClaimRewardFromAPI());
    }

    private IEnumerator ClaimRewardFromAPI()
    {
        if (petId == 0 || string.IsNullOrEmpty(apiUrlSecondary))
        {
            Debug.LogError("❌ Pet ID or API URL not found!");
            yield break;
        }

        // Use secondary API URL with pets route
        string url = $"{apiUrlSecondary}/pets/{petId}/dailyReward";

        // Create JSON body
        string jsonBody = JsonUtility.ToJson(new StreakData { streak = currentStreak });

        using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ Daily reward claimed: {request.downloadHandler.text}");

                // Parse response
                DailyRewardResponse response = JsonUtility.FromJson<DailyRewardResponse>(request.downloadHandler.text);

                if (response.success)
                {
                    string petKey = PlayerPrefKeys.PetPrefix;
                    
                    // Update PlayerPrefs with new values
                    PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetCoins, response.data.coins);
                    PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetFoodStack, response.data.food_stack);

                    // Update login dates
                    string todayDate = DateTime.Now.ToString("yyyy-MM-dd");
                    PlayerPrefs.SetString(LAST_LOGIN_DATE_KEY, todayDate);
                    PlayerPrefs.SetString(LAST_DAILY_REWARD_KEY, todayDate);
                    PlayerPrefs.Save();

                    Debug.Log($"🎉 Reward claimed! Type: {response.data.reward_type}, Amount: {response.data.reward_amount}");
                    
                    // Close the menu
                    dailyRewardPanel.SetActive(false);
                }
            }
            else
            {
                Debug.LogError($"❌ Failed to claim daily reward: {request.error}");
            }
        }
    }

    public int GetCurrentStreak()
    {
        return currentStreak;
    }

    // Helper classes for JSON serialization
    [System.Serializable]
    private class StreakData
    {
        public int streak;
    }

    [System.Serializable]
    private class DailyRewardResponse
    {
        public bool success;
        public string message;
        public RewardData data;
    }

    [System.Serializable]
    private class RewardData
    {
        public int coins;
        public int food_stack;
        public string reward_type;
        public int reward_amount;
    }
}