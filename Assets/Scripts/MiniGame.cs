using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro; // Add this if using TextMeshPro

public class MiniGame : MonoBehaviour
{
    public GameObject[] NavButtons;
    public GameObject[] Toys;
    public Slider GameBar;
    public GameObject TopOfScreen;
    public GameObject GameBG;
    public GameObject ButtonForTap;
    public GameObject SliderGameBar;
    public SpriteRenderer PetSpriteRenderer;
    private bool isGameRunning = false;
    private bool isIncreasing = true;
    public Animator HandAnimation;
    public Animator PetAnimation;
    public GameObject Hand;
    public GameObject BalltoPlay;
    public SpriteRenderer playBall;
    public Sprite[] toysSprite = new Sprite[6];
    private int index;
    public GameObject petObject;
    private PetBehaviour petBehaviour;

    // NEW: Result Panel UI Elements
    public GameObject ResultPanel;
    public TextMeshProUGUI ResultTitleText; // or use "public Text ResultTitleText;" for legacy UI
    public TextMeshProUGUI ResultMessageText; // or use "public Text ResultMessageText;" for legacy UI
    Color titleColor = Color.white;
    private Vector3 successPositionLeft = new Vector3(-2.1f, 3.5f, 0);
    private Vector3 goodCatchPositionLeft = new Vector3(-3.5f, 3.5f, 0);
    private Vector3 badCatchPositionLeft = new Vector3(-4f, 6f, 0);
    private Vector3 successPositionRight = new Vector3(2.2f, 3.5f, 0);
    private Vector3 goodCatchPositionRight = new Vector3(3.5f, 3.5f, 0);
    private Vector3 badCatchPositionRight = new Vector3(4f, 6f, 0);
    private Vector3 initialPosition = new Vector3(0.12f, 0.70f, 0);
    private Vector3 initialScale = new Vector3(2, 2, 1);
    private Vector3 targetScale = new Vector3(1, 1, 1);
    private Vector3 badCatchScale = new Vector3(0.75f, 0.75f, 1);
    private bool isLeft;
    private bool tappable = true;
    public HorizontalPageScroller HPS;
    public GameObject[] accessoryObjects;
    
    private bool isInMiniGame = false;
    private bool[] savedAccessoryStates;

    private int[] additionalIncrement = new int[] { 8, 10, 12, 15, 15, 20 };
    private string lastResult = ""; // Store the result type
    public SoundManager soundManager;

    private void Start()
    {
        petBehaviour = petObject.GetComponent<PetBehaviour>();
        
        // Make sure result panel is hidden at start
        if (ResultPanel != null)
            ResultPanel.SetActive(false);
    }

    void Update()
    {
        if (isGameRunning)
        {
            if (isIncreasing)
            {
                GameBar.value += Time.deltaTime * 60;
                if (GameBar.value >= 100)
                {
                    isIncreasing = false;
                }
            }
            else
            {
                GameBar.value -= Time.deltaTime * 60;
                if (GameBar.value <= 0)
                {
                    isIncreasing = true;
                }
            }
        }

        if (isInMiniGame)
        {
            ForceAccessoriesOff();
        }
    }

    private void ForceAccessoriesOff()
    {
        foreach (var accessory in accessoryObjects)
        {
            if (accessory.activeSelf)
            {
                accessory.SetActive(false);
            }
        }
    }

    public void StartMiniGame(int toyIndex)
    {
        TopOfScreen.SetActive(false);
        GameBG.SetActive(true);
        PetSpriteRenderer.sortingOrder = 3;
        petBehaviour.DisableBehavior();
        StartCoroutine(petBehaviour.WalkToInitialPosition());
        StartCoroutine(waitPetToPosition());
        Hand.SetActive(true);
        tappable = true;

        foreach (var button in NavButtons)
        {
            button.SetActive(false);
        }
        foreach (var toy in Toys)
        {
            toy.GetComponent<Button>().interactable = false;
        }

        savedAccessoryStates = new bool[accessoryObjects.Length];
        for (int i = 0; i < accessoryObjects.Length; i++)
        {
            savedAccessoryStates[i] = accessoryObjects[i].activeSelf;
            accessoryObjects[i].SetActive(false);
        }

        isInMiniGame = true;

        Toys[toyIndex].SetActive(false);
        index = toyIndex;
        Hand.transform.localPosition = new Vector3(0, -4.5f, 0);
        BalltoPlay.SetActive(false);
        SliderGameBar.SetActive(true);
        GameBar.value = 0;
        isGameRunning = true;
        isIncreasing = true;
        ButtonForTap.SetActive(true);

        // Make sure result panel is hidden when starting
        if (ResultPanel != null)
            ResultPanel.SetActive(false);

        // NEW: Switch to minigame BGM (index 1)
        if (soundManager != null)
        {
            soundManager.SwitchToAlternateBGM();
        }
    }

    private IEnumerator waitPetToPosition()
    {
        yield return new WaitForSeconds(1f);
    }

    public void OnScreenTapped()
    {
        if (isGameRunning && tappable)
        {
            float sliderValue = GameBar.value;
            playBall.sprite = toysSprite[index];
            tappable = false;
            StartCoroutine(HandMovementAndToss(sliderValue));
        }
    }

    private IEnumerator HandMovementAndToss(float sliderValue)
    {
        SliderGameBar.SetActive(false);
        float timeElapsed = 0f;
        float moveDuration = 1f;

        Vector3 startPosition = new Vector3(0, -4.5f, 0);
        Vector3 endPosition = new Vector3(0, -4f, 0);

        while (timeElapsed < moveDuration)
        {
            float t = timeElapsed / moveDuration;
            Hand.transform.localPosition = Vector3.Lerp(startPosition, endPosition, t);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        HandAnimation.SetTrigger("GoToss");
        StartCoroutine(WaitAndProceed(sliderValue));
    }

    private IEnumerator WaitAndProceed(float sliderValue)
    {
        yield return new WaitForSeconds(1f);
        BalltoPlay.SetActive(true);
        DetermineSliderRange(sliderValue);
    }

    private IEnumerator BallGo(Vector3 targetPosition, Vector3 targetScale)
    {
        BalltoPlay.transform.localPosition = initialPosition;
        BalltoPlay.transform.localScale = initialScale;

        float timeElapsed = 0f;
        float moveDuration = 1f;

        Vector3 startPosition = BalltoPlay.transform.localPosition;
        Vector3 startScale = BalltoPlay.transform.localScale;
        BalltoPlay.SetActive(true);

        while (timeElapsed < moveDuration)
        {
            float t = timeElapsed / moveDuration;
            BalltoPlay.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            BalltoPlay.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        BalltoPlay.transform.localPosition = targetPosition;
        BalltoPlay.transform.localScale = targetScale;
    }

    private void DetermineSliderRange(float value)
    {
        isGameRunning = false;
        SliderGameBar.SetActive(false);

        isLeft = Random.Range(0, 2) == 0;

        Vector3 targetPosition = initialPosition;
        Vector3 localTargetScale = initialScale;

        if (value >= 40 && value <= 60)
        {
            targetPosition = isLeft ? successPositionLeft : successPositionRight;
            localTargetScale = targetScale;
            lastResult = "perfect";
            HandleSuccess();
        }
        else if ((value >= 30 && value < 40) || (value > 60 && value <= 70))
        {
            targetPosition = isLeft ? goodCatchPositionLeft : goodCatchPositionRight;
            localTargetScale = targetScale;
            lastResult = "good";
            HandleWarning();
        }
        else
        {
            targetPosition = isLeft ? badCatchPositionLeft : badCatchPositionRight;
            localTargetScale = badCatchScale;
            lastResult = "miss";
            HandleFailure();
        }

        if (isLeft)
        {
            PetAnimation.SetBool("goJumpLeft", true);
        }
        else
        {
            PetAnimation.SetBool("goJumpRight", true);
        }

        StartCoroutine(BallGo(targetPosition, localTargetScale));

        StartCoroutine(WaitAndShowResultPanel(3f));
    }

    private IEnumerator WaitAndShowResultPanel(float second)
    {
        yield return new WaitForSeconds(second);

        if (isLeft)
        {
            PetAnimation.SetBool("goJumpLeft", false);
        }
        else
        {
            PetAnimation.SetBool("goJumpRight", false);
        }

        // Show the result panel
        ShowResultPanel();
    }

    private void ShowResultPanel()
    {
        // Update text based on result
        string resultTitle = "";
        string resultMessage = "";

        switch (lastResult)
        {
            case "perfect":
                resultTitle = "You threw the ball!";
                resultMessage = "Perfect! Amazing throw!";
                ColorUtility.TryParseHtmlString("#10b981", out titleColor); // Green
                break;
            case "good":
                resultTitle = "You threw the ball!";
                resultMessage = "Good! Nice try!";
                ColorUtility.TryParseHtmlString("#ffcc00", out titleColor);
                break;
            case "miss":
                resultTitle = "You missed the throw!";
                resultMessage = "Aww. Better luck next time!";
                ColorUtility.TryParseHtmlString("#b91c1c", out titleColor);
                break;
        }

        if (ResultTitleText != null)
            ResultTitleText.text = resultTitle;
            ResultMessageText.color = titleColor;
        if (ResultMessageText != null)
            ResultMessageText.text = resultMessage;

        // Show the panel
        if (ResultPanel != null)
            ResultPanel.SetActive(true);
    }

    // NEW: Button click handlers
    public void OnGoBackClicked()
    {
        if (ResultPanel != null)
            ResultPanel.SetActive(false);
        
        EndMiniGame();
    }

    public void OnPlayAgainClicked()
    {
        if (ResultPanel != null)
            ResultPanel.SetActive(false);
        
        // Reset and start the minigame again
        StartMiniGame(index);
    }

    private void EndMiniGame()
    {
        isInMiniGame = false;
    
        HandAnimation.ResetTrigger("GoToss");
        Hand.SetActive(false);
        Toys[index].SetActive(true);
        ButtonForTap.SetActive(false);
        tappable = true;
        GameBG.SetActive(false);
        TopOfScreen.SetActive(true);
        PetSpriteRenderer.sortingOrder = 1;
    
        RestoreAccessories();
        PetBehaviour.canWalk = true;
        foreach (var button in NavButtons)
        {
            button.SetActive(true);
        }
        foreach (var toy in Toys)
        {
            toy.GetComponent<Button>().interactable = true;
        }
    
        // NEW: Switch back to default BGM (index 0)
        if (soundManager != null)
        {
            soundManager.SwitchToDefaultBGM();
        }
    }

    private void HandleSuccess()
    {
        StartCoroutine(UpdatePlayfulness(8 + additionalIncrement[index], "perfect"));
    }

    private void HandleWarning()
    {
        StartCoroutine(UpdatePlayfulness(5 + additionalIncrement[index], "good"));
    }

    private void HandleFailure()
    {
        StartCoroutine(UpdatePlayfulness(1 + additionalIncrement[index], "miss"));
    }

    private IEnumerator UpdatePlayfulness(int increment, string result)
    {
        string petKey = PlayerPrefKeys.PetPrefix;
        string apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL);
        string apiUrlSecondary = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary, apiUrl + "/api");
        int petId = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);

        string url = $"{apiUrlSecondary}/pets/{petId}/addPlay";

        PlayfulnessPayload payload = new PlayfulnessPayload
        {
            increment = increment,
            result = result
        };

        string jsonData = JsonUtility.ToJson(payload);

        UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT)
        {
            uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {

            string responseText = request.downloadHandler.text;
            PetStatsResponse statsResponse = JsonUtility.FromJson<PetStatsResponse>(responseText);

            StorageBridge.Instance.SaveValue(petKey + PlayerPrefKeys.PetPlayfulness, statsResponse.playfulness);
            StorageBridge.Instance.SaveValue(petKey + PlayerPrefKeys.PetHunger, statsResponse.hunger);
            StorageBridge.Instance.SaveValue(petKey + PlayerPrefKeys.PetHygiene, statsResponse.hygiene);
            StorageBridge.Instance.SaveValue(petKey + PlayerPrefKeys.PetSleep, statsResponse.sleep);
            StorageBridge.Instance.SaveValue(petKey + PlayerPrefKeys.PetCoins, statsResponse.coins);

            int[] stats = new int[] { 
                statsResponse.playfulness, 
                statsResponse.hunger, 
                statsResponse.hygiene, 
                statsResponse.sleep 
            };
            HPS.UpdateButtonFill(stats);
        }
        else
        {
            Debug.LogError("Error updating stats on backend: " + request.error);
        }
    }

    private void RestoreAccessories()
    {
        if (savedAccessoryStates != null && savedAccessoryStates.Length == accessoryObjects.Length)
        {
            for (int i = 0; i < accessoryObjects.Length; i++)
            {
                accessoryObjects[i].SetActive(savedAccessoryStates[i]);
            }
        }
        else
        {
            RestoreAccessoriesFromPlayerPrefs();
        }
    }

    private void RestoreAccessoriesFromPlayerPrefs()
    {
        string petKey = PlayerPrefKeys.PetPrefix;

        int headAccessory = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHead, 0);
        int neckAccessory = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetNeck, 0);
        int eyesAccessory = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetEyes, 0);

        for (int i = 0; i < accessoryObjects.Length; i++)
        {
            if (i == 0)
                accessoryObjects[i].SetActive(headAccessory > 0);
            else if (i == 1)
                accessoryObjects[i].SetActive(eyesAccessory > 0);
            else if (i == 2)
                accessoryObjects[i].SetActive(neckAccessory > 0);
            else
                accessoryObjects[i].SetActive(false);
        }
    }
}

[System.Serializable]
public class PlayfulnessPayload
{
    public int increment;
    public string result;
}

[System.Serializable]
public class PetStatsResponse
{
    public int playfulness;
    public int hunger;
    public int sleep;
    public int hygiene;
    public int coins;
}