using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Networking;
using NUnit.Framework;

public class PetBehaviour : MonoBehaviour
{
    public static PetBehaviour Instance { get; private set; }

    [Header("Pet Movement and Scaling Factors")]
    private float moveSpeed = 1f;
    private float changeDirectionTimeMin = 2f;
    private float changeDirectionTimeMax = 10f;
    private float layTimeMin = 10f;
    private float layTimeMax = 15f;

    [Header("Animator and Controllers")]
    private Animator animator;
    public TMP_Text petNameTxt;
    public RuntimeAnimatorController[] controllers;
    private Pet pet;

    // State variables for behavior
    private float timer;
    private int sleepState = 0; // 0: idle, 1: left lay sleep, 2: right lay sleep, 3: sit sleep left, 4: sit sleep right
    private bool goLeft = false;
    private bool goRight = false;
    private bool isMoving = false;
    private bool isIdle = true;
    private bool isSleeping = false;
    public static bool canWalk = false; // Flag to disable walking and sleeping behaviors

    [Header("Buttons")]
    public GameObject[] buttonsToDisable; // Buttons to disable when pet is doing other actions

    public Animator foodAnimator;

    public Animator FaucerAnimator;

    [Header("Bath Settings")]
    public GameObject[] bubblePrefabs;  // Bubble prefabs
    public Animator ShowerAnimator;
    public GameObject ShowerButton;
    public GameObject SoapQuantity;
    public GameObject SoapNotEnoughPanel;
    public TMP_Text SoapQuantityText;
    public Sprite[] soaps = new Sprite[4];
    public SpriteRenderer soap;
    private bool isTouching = false;
    private static int bubbleCount = 0; // Static counter for bubbles
    private float bubbleFlowDuration = 10f; // Time for the bubbles to move down
    private float bubbleFadeDuration = 7f; // Time for fading the bubbles

    [Header("Sleep Settings")]
    public GameObject lightEffect;
    private bool isLightOn = false;
    private bool FTLIFS = false;
    public StatManager statManager;

    [Header("Accessory Settings")]
    public GameObject[] accessoryObjects; // Array of accessory GameObjects to toggle
    public Sprite[] Hats;
    public Sprite[] Glasses;
    public Sprite[] Collars;
    public Sprite[] SideCollars;
    public Sprite[] SideGlasses;
    private bool isHatOn = false;
    private bool isCollarOn = false;
    private bool isGlassesOn = false;
    private Vector3 glassesOriginalPosition;
    private Vector3 glassesOriginalScale;
    [Header("Audio Source")]
    public SoundManager SM;
    public TMP_Text foodStack;
    public TMP_Text[] Coins;
    public HorizontalPageScroller HPS;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        pet = new Pet();
        reflectPetData();
        SetInitialPosition();
        glassesOriginalPosition = accessoryObjects[2].transform.localPosition;
        glassesOriginalScale = accessoryObjects[2].transform.localScale;
        if (PlayerPrefs.GetInt(PlayerPrefKeys.isSleeping, 0) == 1)
        {
            ToggleLightEffect();
        } else
        {
            FTLIFS = true;
        }
    }

    public void reflectPetData()
    {
        string petKey = PlayerPrefKeys.PetPrefix; // Unique pet key, assuming only one pet

        // Retrieve data from PlayerPrefs and assign it to the pet object
        pet.id = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);
        pet.student_id = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetStudentID);
        pet.pet_name = PlayerPrefs.GetString(petKey + PlayerPrefKeys.PetName);
        pet.pet_type = PlayerPrefs.GetString(petKey + PlayerPrefKeys.PetType);
        pet.coins = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetCoins);
        pet.food_stack = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetFoodStack);
        pet.pet_head = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHead, 0);
        pet.pet_neck = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetNeck, 0);
        pet.pet_eyes = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetEyes, 0);
        pet.hunger = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHunger);
        pet.playfulness = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetPlayfulness);
        pet.hygiene = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHygiene);
        pet.sleep = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetSleep);
        pet.created_at = PlayerPrefs.GetString(petKey + PlayerPrefKeys.PetCreatedAt);
        pet.updated_at = PlayerPrefs.GetString(petKey + PlayerPrefKeys.PetUpdatedAt);
        switch (pet.pet_type)
        {
            case "cat_1": animator.runtimeAnimatorController = controllers[0]; break;
            case "dog_1": animator.runtimeAnimatorController = controllers[3]; break;
            default: animator.runtimeAnimatorController = controllers[0]; break;
        }

        petNameTxt.text = pet.pet_name;
        foodStack.text = pet.food_stack.ToString() + "x";
        foreach (var coins in Coins)
        {
            coins.text = pet.coins.ToString();
        }
        int[] stats = new int[] { pet.playfulness, pet.hunger, pet.hygiene, pet.sleep };
        HPS.UpdateButtonFill(stats);

        // Set accessories based on saved data
        isHatOn = pet.pet_head > 0;
        isCollarOn = pet.pet_neck > 0;
        isGlassesOn = pet.pet_eyes > 0;

        if (accessoryObjects.Length >= 3)
        {
            accessoryObjects[0].SetActive(isHatOn); // Hat
            accessoryObjects[1].SetActive(isCollarOn); // Collar
            accessoryObjects[2].SetActive(isGlassesOn); // Glasses
        }
        
        for (int i = 0; i < accessoryObjects.Length; i++)
        {
            switch (i)
            {
                case 0: accessoryObjects[i].GetComponent<SpriteRenderer>().sprite = pet.pet_head-1 >= 0 ? Hats[pet.pet_head-1] : Hats[0] ; break;
                case 1: accessoryObjects[i].GetComponent<SpriteRenderer>().sprite = pet.pet_eyes-5 >= 0 ? Glasses[pet.pet_eyes-5] : Glasses[0] ; break;
                case 2: accessoryObjects[i].GetComponent<SpriteRenderer>().sprite = pet.pet_neck-9 >= 0 ? Collars[pet.pet_neck-9] : Collars[0]; break;
                case 3: accessoryObjects[i].GetComponent<SpriteRenderer>().sprite = pet.pet_neck-9 >= 0 ? SideCollars[pet.pet_neck-9] : SideCollars[0]; break;
                case 4: accessoryObjects[i].GetComponent<SpriteRenderer>().sprite = pet.pet_neck-5 >= 4 ? SideCollars[pet.pet_neck-5] : SideCollars[4]; break;
                case 5: accessoryObjects[i].GetComponent<SpriteRenderer>().sprite = pet.pet_eyes-5 >= 0 ? SideGlasses[pet.pet_eyes-5] : SideGlasses[0]; break;
                case 6: accessoryObjects[i].GetComponent<SpriteRenderer>().sprite = pet.pet_eyes-1 >= 4 ? SideGlasses[pet.pet_eyes-1] : SideGlasses[0]; break;
                case 7: accessoryObjects[i].GetComponent<SpriteRenderer>().sprite = pet.pet_head-1 >= 0 ? Hats[pet.pet_head-1] : Hats[0] ; break;
                case 8: accessoryObjects[i].GetComponent<SpriteRenderer>().sprite = pet.pet_head-1 >= 0 ? Hats[pet.pet_head-1] : Hats[0] ; break;
            }   
        }
    }

    private void SetInitialPosition()
    {
        // Set the initial position of the pet at x = 0, y = -2.25
        transform.position = new Vector3(0f, -2.25f, 0f);

        // Set the initial scale of the pet to 0.33 (idle state scale)
        transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        // Set initial state to idle
        animator.SetInteger("sleepType", 0); // Idle state initially
    }

    // Update is called once per frame
    void Update()
    {
        if (canWalk && !isSleeping)
        {
            HandleBehaviorTimers();
            AdjustScaleBasedOnPosition(); // Adjust scale based on current position
        }
    }

    private void HandleBehaviorTimers()
    {
        // Randomize the behavior based on the time intervals
        if (timer <= 0f)
        {
            timer = Random.Range(changeDirectionTimeMin, changeDirectionTimeMax);

            // Randomly choose a behavior (walk, sleep, etc.)
            int behaviorChoice = Random.Range(0, 100); // A value between 0 and 100

            if (behaviorChoice < 15) // 15% chance for sleep
            {
                // Sleep
                isIdle = false;
                isMoving = false;
                StartSleep();
            }
            else if (behaviorChoice < 50) // 30% chance for walking
            {
                // Walk
                isIdle = false;
                isMoving = true;
                StartWalk();
            }
            else // 55% chance for idle
            {
                // Idle
                isIdle = true;
                isMoving = false;
                StartIdle();
            }
        }
        else
        {
            timer -= Time.deltaTime;
        }
    }

    private void StartIdle()
    {
        sleepState = 0; // Set to idle state
        animator.SetBool("goLeft", false);
        animator.SetBool("goRight", false);
        animator.SetInteger("sleepType", sleepState);
        isIdle = true;

        StartCoroutine(idleAccessories());

        // Set the idle scale to 0.33 (fixed value for idle state)
        transform.localScale = new Vector3(0.5f, 0.5f, 1f);
    }

    private IEnumerator idleAccessories()
    {
        yield return new WaitForSeconds(.1f);
        for (int i = 0; i < accessoryObjects.Length; i++)
        {
            if (i > 2)
            {
                accessoryObjects[i].SetActive(false);
                continue;
            }
            if (isHatOn && i == 0)
            {
                accessoryObjects[i].SetActive(true);
            }
            if (isGlassesOn && i == 1)
            {
                accessoryObjects[i].SetActive(true);
            }
            if (isCollarOn && i == 2)
            {
                accessoryObjects[i].SetActive(true);
            }
        }
    }
    
    private IEnumerator walkingAccessories(bool right)
    {
        yield return new WaitForSeconds(.1f);
        for (int i = 0; i < accessoryObjects.Length; i++)
        {
            accessoryObjects[i].SetActive(false);
            if (!goLeft && ((isCollarOn && i == 3) || (isGlassesOn && i == 5) || (isHatOn && i == 7)))
            {
                accessoryObjects[i].SetActive(true);
                continue;
            }
            else if (goLeft && ((isCollarOn && i == 4) || (isGlassesOn && i == 6) || (isHatOn && i == 8)))
            {
                accessoryObjects[i].SetActive(true);
                continue;
            }
            accessoryObjects[i].SetActive(false);
        }
    }

    private void StartWalk()
    {
        sleepState = 0; // Set to idle state
        animator.SetBool("goLeft", false);
        animator.SetBool("goRight", false);
        animator.SetInteger("sleepType", sleepState);

        // Determine direction based on current X position compared to target position
        float targetX = Random.Range(-1.91f, 1.91f);
        if (targetX < transform.position.x)
        {
            // Move left if the target X position is smaller than the current X position
            goLeft = true;
            goRight = false;
        }
        else
        {
            // Move right if the target X position is larger than the current X position
            goLeft = false;
            goRight = true;
        }

        StartCoroutine(walkingAccessories(goRight));

        // Set walking direction parameters
        animator.SetBool("goLeft", goLeft);
        animator.SetBool("goRight", goRight);

        // Initiate walk behavior
        StartCoroutine(WalkToRandomPosition(goLeft));
    }

    private void StartSleep()
    {
        isSleeping = true;

        // Random sleep position
        int sleepChoice = Random.Range(3, 5); // 1 - left lay sleep, 2 - right lay sleep, 3 - sit sleep left, 4 - sit sleep right
        sleepState = sleepChoice;
        animator.SetInteger("sleepType", sleepState); // Update sleep state for animator
        foreach (var accessory in accessoryObjects)
        {
            accessory.SetActive(false); // Hide all accessories when sleeping
        }

        // Use layTimeMin and layTimeMax to dictate how long the pet sleeps
        float sleepDuration = Random.Range(layTimeMin, layTimeMax);
        StartCoroutine(LayForDuration(sleepDuration));
    }

    private IEnumerator WalkToRandomPosition(bool isWalkingLeft)
    {
        float targetX = Random.Range(-1.91f, 1.91f);
        float targetY = Random.Range(-2.55f, -1.88f); // Target Y range for movement

        // Move the pet toward the target
        float elapsedTime = 0f;
        Vector3 startingPosition = transform.position;
        Vector3 targetPosition = new Vector3(targetX, targetY, 0f);

        // Move pet toward target
        while (elapsedTime < 1f)
        {
            float distanceCovered = (elapsedTime / 1f) * moveSpeed;
            transform.position = Vector3.Lerp(startingPosition, targetPosition, distanceCovered);
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        // Once movement ends, return to idle state
        isMoving = false;
        StartIdle();
    }

    private IEnumerator LayForDuration(float duration)
    {
        // Wait for the specified sleep duration (using layTimeMin and layTimeMax)
        yield return new WaitForSeconds(duration);

        isSleeping = false;
        StartIdle(); // Once sleep ends, go idle
    }

    private void AdjustScaleBasedOnPosition()
    {
        // Calculate the scale ratio based on the Y position
        float currentY = transform.position.y;

        // Define a range for Y position (from -2.55 to -1.88 for backward movement)
        float normalizedY = Mathf.InverseLerp(-2.55f, -1.88f, currentY);

        // Calculate the new scale based on the normalized Y value
        float idleScale = Mathf.Lerp(0.5f, 0.15f, normalizedY); // Idle scale range from 0.33 to 0.15
        float movementScale = Mathf.Lerp(0.5f, 0.2f, normalizedY); // Movement scale range from 0.5 to 0.2

        // Apply the new scale to the pet
        if (isIdle)
        {
            transform.localScale = new Vector3(idleScale, idleScale, 1f); // Apply idle scale
        }
        else if (isMoving || isSleeping) // Apply movement scale for all movement states, including sleeping
        {
            transform.localScale = new Vector3(movementScale, movementScale, 1f); // Apply movement scale
        }
    }

    public void DisableBehavior()
    {
        // Disable movement
        canWalk = false;
        isMoving = false;
        isIdle = true;

        // Stop all movement animations
        animator.SetBool("goLeft", false);
        animator.SetBool("goRight", false);
        animator.SetInteger("sleepType", 0);
    }

    public IEnumerator WalkToInitialPosition()
    {
        // Store the initial position
        Vector3 initialPosition = new Vector3(0f, -2.25f, 0f); // Assuming this is the initial position

        // Set walking animation
        animator.SetBool("goLeft", initialPosition.x < transform.position.x); // Decide walking direction based on X position
        animator.SetBool("goRight", initialPosition.x > transform.position.x); // Decide walking direction based on X position

        // Move the pet smoothly toward the initial position
        float moveDuration = 2f; // Time it will take to walk back to initial position (you can adjust this value)
        float elapsedTime = 0f;
        Vector3 startingPosition = transform.position;

        while (elapsedTime < moveDuration)
        {
            transform.position = Vector3.Lerp(startingPosition, initialPosition, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Final position snap to ensure accuracy
        transform.position = initialPosition;

        // Once the pet reaches the initial position, go idle
        StartIdle();
    }

    public void OnFeedButtonPressed()
    {
        if (foodAnimator != null)
        {
            foodAnimator.SetTrigger("GoEat");
            animator.SetBool("eat", true);
            transform.localScale = new Vector3(0.7f, 0.7f, 1f); // Set scale to 0.5 when eating
            for (int i = 0; i < accessoryObjects.Length; i++)
            {
                if (i == 2)
                {
                    accessoryObjects[i].transform.localScale = new Vector3(accessoryObjects[i].transform.localScale.x * 0.5f, accessoryObjects[i].transform.localScale.y * 0.5f, 1f); // Enlarge glasses when eating
                    accessoryObjects[i].transform.localPosition = new Vector3(-0.25f, 0.025f, 1f);
                    continue;
                }
                accessoryObjects[i].SetActive(false); // Hide all accessories when eating
            }
        }
        SM.PlayEatingSoundForDuration(8f);
        // Disable buttons during feeding animation
        foreach (var button in buttonsToDisable)
        {
            button.SetActive(false);
        }

        // Re-enable buttons after a delay (assuming feeding animation lasts 3 seconds)
        StartCoroutine(ReenableButtonsAfterDelay(10f));
    }

    private void OnFinishFeeding()
    {
        if (foodAnimator != null)
        {
            foodAnimator.ResetTrigger("GoEat");
            animator.SetBool("eat", false);
            transform.localScale = new Vector3(0.5f, 0.5f, 1f); // Reset scale to idle state
            for (int i = 0; i < accessoryObjects.Length; i++)
            {
                if (i > 2)
                {
                    accessoryObjects[i].SetActive(false);
                    continue;
                }
                if (isHatOn && i == 0)
                {
                    accessoryObjects[i].SetActive(true);
                }
                if (isGlassesOn && i == 1)
                {
                    accessoryObjects[i].SetActive(true);
                }
                if (isCollarOn && i == 2)
                {
                    accessoryObjects[i].SetActive(true);
                }
            }
            accessoryObjects[2].transform.localPosition = glassesOriginalPosition;
            accessoryObjects[2].transform.localScale = glassesOriginalScale;
            int playfulness = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetPlayfulness);
            int hunger = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHunger) + 10;
            if (hunger >= 100) {
                hunger = 100;
            }
            int bath = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHygiene);  // Assuming bath is stored in hygiene
            int sleep = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetSleep);

            StorageBridge.Instance.SaveValue(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHunger, hunger);
            // Create an array with the updated stats
            int[] stats = new int[] { playfulness, hunger, bath, sleep };
        
        // Call UpdateButtonFill with the stats array
        HPS.UpdateButtonFill(stats);
        }
    }

    private IEnumerator ReenableButtonsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        foreach (var button in buttonsToDisable)
        {
            button.SetActive(true);
        }
        OnFinishFeeding();
    }

    // Detect collision with soap object
    // Replace your collision detection methods with these:

    // Detect when soap enters collision
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("soap"))
        {
            foreach (var acc in accessoryObjects) 
            {
                acc.SetActive(false);
            }
            foreach (var button in buttonsToDisable)
            {
                button.SetActive(false); // Disable buttons when touching soap
            }

            if (!isTouching)
            {
                int currentCoins = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetCoins);
                if (currentCoins <= 2)
                {
                    SoapNotEnoughPanel.SetActive(true);
                    for (int i = 0; i < accessoryObjects.Length; i++)
                    {
                        if (i > 2)
                        {
                            accessoryObjects[i].SetActive(false);
                            continue;
                        }
                        if (isHatOn && i == 0)
                        {
                            accessoryObjects[i].SetActive(true);
                        }
                        if (isGlassesOn && i == 1)
                        {
                            accessoryObjects[i].SetActive(true);
                        }
                        if (isCollarOn && i == 2)
                        {
                            accessoryObjects[i].SetActive(true);
                        }
                    }

                    foreach (var button in buttonsToDisable)
                    {
                        button.SetActive(true);  // Enable any buttons that were disabled
                    }
                    return;
                }

                isTouching = true;
                SoapQuantity.SetActive(false);
                // Start the continuous bubble spawning coroutine
                StartCoroutine(ContinuouslySpawnBubbles());
            }
        }
    }

    // Detect when soap exits collision
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("soap"))
        {
            isTouching = false; // Stop spawning when soap exits
        }
    }

    // Coroutine to continuously spawn bubbles while soap is touching
    private IEnumerator ContinuouslySpawnBubbles()
    {
        // Get the bounds of the pet's collider
        Collider2D petCollider = GetComponent<Collider2D>();
        Bounds colliderBounds = petCollider.bounds;

        // Keep spawning bubbles while soap is touching and limit hasn't been reached
        while (isTouching && bubbleCount < 10)
        {
            bubbleCount++; // Increment the bubble count
            
            if (bubbleCount % 2 == 1)
            {
                SM.PlayBubble(); // Plays on: 1, 3, 5, 7, 9
            }

            // Generate random positions within the collider's bounds
            float randomX = Random.Range(colliderBounds.min.x, colliderBounds.max.x);
            float randomY = Random.Range(colliderBounds.min.y, colliderBounds.max.y);
            float randomX2 = Random.Range(colliderBounds.min.x, colliderBounds.max.x);
            float randomY2 = Random.Range(colliderBounds.min.y, colliderBounds.max.y);

            // Create spawn positions with the random offsets
            Vector2 spawnPosition = new Vector2(randomX, randomY);
            Vector2 spawnPosition2 = new Vector2(randomX2, randomY2);

            // Instantiate the bubbles at the random positions
            Instantiate(bubblePrefabs[Random.Range(0, bubblePrefabs.Length)], spawnPosition, Quaternion.identity);
            Instantiate(bubblePrefabs[Random.Range(0, bubblePrefabs.Length)], spawnPosition2, Quaternion.identity);

            // Wait for 0.5 seconds before spawning the next set of bubbles
            // Adjust this delay value to control spawn speed
            yield return new WaitForSeconds(0.2f);
        }

        // Once bubble limit is reached, show the shower button
        if (bubbleCount >= 10)
        {
            ShowerButton.SetActive(true);
        }

        isTouching = false; // Reset the flag
    }

    public void OpenShower()
    {
        StartCoroutine(ShowerRoutine());
    }

    // Shower routine that moves bubbles down and fades them
    private IEnumerator ShowerRoutine()
    {
        // Trigger the shower animation
        ShowerAnimator.SetTrigger("ShowerOn");
        SM.PlayShowerSoundForDuration(8f);
        // Move and fade the bubbles during this time
        MoveAndFadeBubbles();
    
        yield return new WaitForSeconds(10f);  // Shower duration
    
        // Reset the trigger after shower animation completes
        ShowerAnimator.ResetTrigger("ShowerOn");
        yield return new WaitForSeconds(1f);  // Wait for animation to finish
    
        // Reset and clean up accessories and buttons after the shower
        for (int i = 0; i < accessoryObjects.Length; i++)
        {
            if (i > 2)
            {
                accessoryObjects[i].SetActive(false);
                continue;
            }
            if (isHatOn && i == 0)
            {
                accessoryObjects[i].SetActive(true);
            }
            if (isGlassesOn && i == 1)
            {
                accessoryObjects[i].SetActive(true);
            }
            if (isCollarOn && i == 2)
            {
                accessoryObjects[i].SetActive(true);
            }
        }
        foreach (var button in buttonsToDisable)
        {
            button.SetActive(true);  // Enable any buttons that were disabled
        }
        // Call the API to update soap usage on the backend
        yield return StartCoroutine(UpdateSoapUsageOnBackend());

        // Call the DataManager API to sync pet data after the shower routine
        DataManager.Instance.StartCoroutine(DataManager.Instance.HandlePetDataRequest());
        ShowerButton.SetActive(false);
        bubbleCount = 0;  // Reset bubble count (if needed)
    }
    
    // Method to update soap usage on the backend
    private IEnumerator UpdateSoapUsageOnBackend()
    {
        // Retrieve API URL and pet ID from PlayerPrefs
        string petKey = PlayerPrefKeys.PetPrefix;
        string apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL);
        string apiUrlSecondary = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary);
        int petId = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);

        // Construct the API URL for soap usage
        string url = $"{apiUrlSecondary}/pets/{petId}/soapuse";

        // Create payload object (no soap_type required now)
        var payload = new object();  // No need to send soap_type

        // Convert to JSON (empty object since no soap_type is needed)
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
            // Update soap quantity in PlayerPrefs
            int currentCoins = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetCoins);
            StorageBridge.Instance.SaveValue(petKey + PlayerPrefKeys.PetCoins, currentCoins - 3);

            foreach (var coin in Coins)
            {
                coin.text = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetCoins).ToString();
            }
            // Retrieve and update stats
            int playfulness = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetPlayfulness);
            int hunger = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHunger);
            int bath = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHygiene) + 10;  // Assuming bath is stored in hygiene
            int sleep = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetSleep);
            StorageBridge.Instance.SaveValue(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHygiene, bath);
            // Create an array with the updated stats
            int[] stats = new int[] { playfulness, hunger, bath, sleep };

            // Call UpdateButtonFill with the stats array
            HPS.UpdateButtonFill(stats);
        }
        else
        {
            Debug.LogError("Error updating soap usage on backend: " + request.error);
        }
        SoapQuantity.SetActive(true);
    }
 
    // Function to move and fade the bubbles
    private void MoveAndFadeBubbles()
    {
        GameObject[] existingBubbles = GameObject.FindGameObjectsWithTag("Bubbles");

        foreach (var bubble in existingBubbles)
        {
            StartCoroutine(MoveAndFadeBubble(bubble));  // Start moving and fading each bubble
        }
    }

    // Move and fade bubble to y = -3.5, then destroy after fade
    private IEnumerator MoveAndFadeBubble(GameObject bubble)
    {
        SpriteRenderer bubbleRenderer = bubble.GetComponent<SpriteRenderer>();  // Get the bubble's sprite renderer

        Vector3 startPosition = bubble.transform.position;
        Vector3 targetPosition = new Vector3(bubble.transform.position.x, -3.5f, bubble.transform.position.z);

        float moveDuration = bubbleFlowDuration;  // Duration for downward movement
        float fadeDuration = bubbleFadeDuration;  // Duration for fading

        float moveElapsedTime = 0f;
        float fadeElapsedTime = 0f;  // Track fade time separately

        // Move and fade the bubble simultaneously
        while (moveElapsedTime < moveDuration)
        {
            // Move the bubble downwards to the target position
            bubble.transform.position = Vector3.Lerp(startPosition, targetPosition, moveElapsedTime / moveDuration);

            // Fade the bubble based on elapsed time
            float fadeProgress = fadeElapsedTime / fadeDuration;
            bubbleRenderer.color = new Color(bubbleRenderer.color.r, bubbleRenderer.color.g, bubbleRenderer.color.b, Mathf.Lerp(1f, 0f, fadeProgress));

            moveElapsedTime += Time.deltaTime;
            fadeElapsedTime += Time.deltaTime;

            yield return null;
        }

        // After the move is complete, ensure the final position and fully transparent
        bubble.transform.position = targetPosition;
        bubbleRenderer.color = new Color(bubbleRenderer.color.r, bubbleRenderer.color.g, bubbleRenderer.color.b, 0f);

        // Destroy the bubble after fading
        Destroy(bubble);
    }

    public void ToggleLightEffect()
    {
        isLightOn = !isLightOn;
        lightEffect.SetActive(isLightOn);
        foreach (var button in buttonsToDisable)
        {
            button.SetActive(!isLightOn);
        }
        SM.PlayLightSound();
        if (isLightOn)
        {
            statManager.isSleeping = true;
            int sleepChoice = Random.Range(1, 5); // 3 - sit sleep left, 4 - sit sleep right
            sleepState = sleepChoice;
            transform.localScale = new Vector3(0.5f, 0.5f, 1f); // Set scale to 0.5 when sleeping
            animator.SetInteger("sleepType", sleepState); // Update sleep state for animator
            foreach (var accessory in accessoryObjects)
            {
                accessory.SetActive(false); // Hide all accessories when sleeping
            }
            isSleeping = true;
            canWalk = false;
            StorageBridge.Instance.SaveValue(PlayerPrefKeys.isSleeping, isSleeping ? 1 : 0);
            
        }
        else
        {
            statManager.isSleeping = false;
            isSleeping = false;
            foreach (var button in buttonsToDisable)
            {
                button.SetActive(true);
            }
            StorageBridge.Instance.SaveValue(PlayerPrefKeys.isSleeping, isSleeping ? 1 : 0);
            StartIdle();
        }
        if (!FTLIFS)
        {
            FTLIFS = true;
        } else
        {
            StartCoroutine(toggleSleepOnBackend(isSleeping));
        }
    }

    private IEnumerator toggleSleepOnBackend(bool isSleeping)
    {
        // Get the pet ID from PlayerPrefs or another source
        string petKey = PlayerPrefKeys.PetPrefix;
        int petID = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);

        // Construct the URL to your backend API (Make sure the URL is correct)
        string apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL);
        string url = $"{apiUrl}/toggle-pet-sleep";  // Change this to your actual API endpoint

        // Create the JSON payload
        SleepPayload jsonPayload = new SleepPayload
        {
            petId = petID,
            isSleep = isSleeping  // Send the sleep state
        };

        // Convert the payload to JSON string
        string jsonString = JsonUtility.ToJson(jsonPayload);
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] jsonToSend = System.Text.Encoding.UTF8.GetBytes(jsonString);
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Send the request and wait for the response
        yield return request.SendWebRequest();
    }

    public void FaucetFlow()
    {
        FaucerAnimator.SetBool("flow", true);
        SM.PlayFaucet();
        StartCoroutine(waitForFaucet());
    }
    
    private IEnumerator waitForFaucet()
    {
        yield return new WaitForSeconds(12f);
        FaucerAnimator.SetBool("flow", false);
    }
}
[System.Serializable]
public class SleepPayload
{
    public int petId;
    public bool isSleep;
}