using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MiniGame : MonoBehaviour
{
    public GameObject[] NavButtons;
    public GameObject[] Toys;
    public Slider GameBar;
    public GameObject ButtonForTap;
    public GameObject SliderGameBar;
    private bool isGameRunning = false;
    private bool isIncreasing = true;
    public Animator HandAnimation;
    public Animator PetAnimation;
    public GameObject Hand;
    public GameObject BalltoPlay;
    public SpriteRenderer playBall;
    public Sprite[] toysSprite = new Sprite[6];
    private int index;
    public GameObject petObject; // Reference to the pet GameObject
    private PetBehaviour petBehaviour; // Reference to the PetBehaviour script

    // Define the left-side positions (negative x values)
    private Vector3 successPositionLeft = new Vector3(-2.1f, 3.5f, 0);
    private Vector3 goodCatchPositionLeft = new Vector3(-3.5f, 3.5f, 0);
    private Vector3 badCatchPositionLeft = new Vector3(-4f, 6f, 0);

    // Define the right-side positions (positive x values)
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

    private void Start()
    {
        petBehaviour = petObject.GetComponent<PetBehaviour>();
    }

    public void StartMiniGame(int toyIndex)
    {
        petBehaviour.DisableBehavior(); // Disable pet behavior when leaving page 0
        StartCoroutine(petBehaviour.WalkToInitialPosition());
        StartCoroutine(waitPetToPosition());
        Hand.SetActive(true);
        // Deactivate NavButtons
        foreach (var button in NavButtons)
        {
            button.SetActive(false);
        }
        foreach (var toy in Toys)
        {
            toy.GetComponent<Button>().interactable = false;
        }
        // Activate the specific toy button to start the game
        Toys[toyIndex].SetActive(false);
        index = toyIndex;
        Hand.transform.localPosition = new Vector3(0, -4.5f, 0);
        BalltoPlay.SetActive(false);
        SliderGameBar.SetActive(true);
        // Start the slider movement
        GameBar.value = 0;
        isGameRunning = true;
        isIncreasing = true;
        ButtonForTap.SetActive(true);
    }

    private IEnumerator waitPetToPosition()
    {
        yield return new WaitForSeconds(1f);
    }

    void Update()
    {
        if (isGameRunning)
        {
            // Update the slider's value (moves from 0 to 100, then back to 0)
            if (isIncreasing)
            {
                GameBar.value += Time.deltaTime * 125; // Adjust the speed as needed
                if (GameBar.value >= 100)
                {
                    isIncreasing = false; // Change direction when slider reaches 100
                }
            }
            else
            {
                GameBar.value -= Time.deltaTime * 125; // Adjust the speed as needed
                if (GameBar.value <= 0)
                {
                    isIncreasing = true; // Change direction when slider reaches 0
                }
            }
        }
    }

    public void OnScreenTapped()
    {
        // Only respond to the tap if the game is running
        if (isGameRunning && tappable)
        {
            // Determine the slider's value and call the corresponding function
            float sliderValue = GameBar.value;
            playBall.sprite = toysSprite[index];
            tappable = false;
            // Start the hand movement coroutine, then proceed to toss
            StartCoroutine(HandMovementAndToss(sliderValue));
        }
    }

    private IEnumerator HandMovementAndToss(float sliderValue)
    {
        SliderGameBar.SetActive(false);
        // Move the hand to the toss position before triggering the animation
        float timeElapsed = 0f;
        float moveDuration = 1f;

        Vector3 startPosition = new Vector3(0, -4.5f, 0);
        Vector3 endPosition = new Vector3(0, -4f, 0);

        while (timeElapsed < moveDuration)
        {
            float t = timeElapsed / moveDuration;
            Hand.transform.localPosition = Vector3.Lerp(startPosition, endPosition, t);
            timeElapsed += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // After the hand movement, trigger the toss animation
        HandAnimation.SetTrigger("GoToss");

        // Start the ball and wait for the animation to complete
        StartCoroutine(WaitAndProceed(sliderValue));  // Wait before proceeding to next actions
    }
    private IEnumerator WaitAndProceed(float sliderValue)
    {
        // Wait for the toss animation or other actions to finish (1 second here)
        yield return new WaitForSeconds(1f);

        // Ball goes after waiting
        BalltoPlay.SetActive(true);

        // Determine the result based on slider value
        DetermineSliderRange(sliderValue);
    }

    private IEnumerator BallGo(Vector3 targetPosition, Vector3 targetScale)
    {
        // Reset the ball's position and scale to its initial local state
        BalltoPlay.transform.localPosition = initialPosition;  // Using localPosition to move relative to parent
        BalltoPlay.transform.localScale = initialScale;

        float timeElapsed = 0f;
        float moveDuration = 1f;  // Adjust this duration for smoother or faster movement

        Vector3 startPosition = BalltoPlay.transform.localPosition;
        Vector3 startScale = BalltoPlay.transform.localScale;
        // Ensure the ball is active before starting the movement
        BalltoPlay.SetActive(true);

        // While the movement is in progress, smoothly transition from the start to the target position and scale
        while (timeElapsed < moveDuration)
        {
            float t = timeElapsed / moveDuration;  // Normalize time to smoothly interpolate

            // Smoothly interpolate position in local space (relative to the parent)
            BalltoPlay.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);

            // Smoothly interpolate scale
            BalltoPlay.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            timeElapsed += Time.deltaTime;  // Increment the time elapsed
            yield return null;  // Wait for the next frame
        }

        // Ensure the ball reaches the exact target position and scale after the movement is complete
        BalltoPlay.transform.localPosition = targetPosition;
        BalltoPlay.transform.localScale = targetScale;

        // Log the final position to ensure it is correct
        Debug.Log("Final Position: " + BalltoPlay.transform.localPosition);
    }

    private Vector3 GetRandomizedTargetPosition(Vector3 leftPosition, Vector3 rightPosition)
    {
        // Randomly choose between the left and right position
        bool goLeft = Random.Range(0, 2) == 0;  // Randomly select 0 or 1
        isLeft = goLeft;
        // Return the chosen position based on the random value
        return goLeft ? leftPosition : rightPosition;
    }

    private void DetermineSliderRange(float value)
    {
        isGameRunning = false;
        SliderGameBar.SetActive(false);

        Vector3 targetPosition = initialPosition;
        Vector3 localTargetScale = initialScale;

        // Randomize the position (left or right)
        Vector3 successTarget = GetRandomizedTargetPosition(successPositionLeft, successPositionRight);
        Vector3 goodCatchTarget = GetRandomizedTargetPosition(goodCatchPositionLeft, goodCatchPositionRight);
        Vector3 badCatchTarget = GetRandomizedTargetPosition(badCatchPositionLeft, badCatchPositionRight);

        if (value >= 40 && value <= 60)
        {
            targetPosition = successTarget;
            localTargetScale = targetScale;
            HandleSuccess();
        }
        else if ((value >= 30 && value < 40) || (value > 60 && value <= 70))
        {
            targetPosition = goodCatchTarget;
            localTargetScale = targetScale;
            HandleWarning();
        }
        else
        {
            targetPosition = badCatchTarget;
            localTargetScale = badCatchScale;
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
        // Start the ball movement after determining the slider range
        StartCoroutine(BallGo(targetPosition, localTargetScale));

        foreach (var toy in Toys)
        {
            toy.GetComponent<Button>().interactable = true;
        }

        // Wait for a while before completing the game
        StartCoroutine(waitforsec(3f));  // Wait for 3 seconds before finishing the game
    }

    private IEnumerator waitforsec(float second)
    {
        yield return new WaitForSeconds(second);
        Debug.Log("Waited for " + second);

        if (isLeft)
        {
            PetAnimation.SetBool("goJumpLeft", false);
        }
        else
        {
            PetAnimation.SetBool("goJumpRight", false);
        }
        // Reset everything after the animation and actions
        HandAnimation.ResetTrigger("GoToss");
        Hand.SetActive(false);
        Toys[index].SetActive(true);
        ButtonForTap.SetActive(false);
        tappable = true;
        foreach (var button in NavButtons)
        {
            button.SetActive(true);
        }
    }

    private void HandleSuccess()
    {
        StartCoroutine(UpdatePlayfulness(8));
    }

    private void HandleWarning()
    {
        StartCoroutine(UpdatePlayfulness(8));
    }

    private void HandleFailure()
    {
        StartCoroutine(UpdatePlayfulness(8));
    }

    private IEnumerator UpdatePlayfulness(int increment)
    {
        // Retrieve API URL and pet ID from PlayerPrefs
        string petKey = PlayerPrefKeys.PetPrefix;
        string apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL);
        string apiUrlSecondary = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary, apiUrl + "/api");
        int petId = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);
        
        // Construct the API URL for updating playfulness
        string url = $"{apiUrlSecondary}/pets/{petId}/addPlay";

        // Create the payload using the PlayfulnessPayload class
        PlayfulnessPayload payload = new PlayfulnessPayload();
        payload.increment = increment;  // Set the increment value

        // Serialize the payload to JSON using JsonUtility
        string jsonData = JsonUtility.ToJson(payload);

        // Create a UnityWebRequest to make a PUT request
        UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT)
        {
            uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Content-Type", "application/json");

        // Send the request and wait for the response
        yield return request.SendWebRequest();

        // Handle the response
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Playfulness updated successfully on the backend.");

            // Retrieve current stats from PlayerPrefs and update playfulness
            int currentPlayfulness = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetPlayfulness) + increment;
            if (currentPlayfulness >= 100)
            {
                currentPlayfulness = 100;
            }
            int hungry = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHunger);
            int hygiene = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHygiene);
            int sleep = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetSleep);

            // Save the updated playfulness to PlayerPrefs
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetPlayfulness, currentPlayfulness);
            PlayerPrefs.Save();

            // Update UI or other necessary elements with the new stats
            int[] stats = new int[] { currentPlayfulness, hungry, hygiene, sleep };
            HPS.UpdateButtonFill(stats);
        }
        else
        {
            Debug.LogError("Error updating playfulness on backend: " + request.error);
        }
    }
}

[System.Serializable]
public class PlayfulnessPayload
{
    public int increment;  // The increment value for playfulness
}