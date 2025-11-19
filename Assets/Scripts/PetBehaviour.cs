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
    private int sleepState = 0;
    private bool goLeft = false;
    private bool goRight = false;
    private bool isMoving = false;
    private bool isIdle = true;
    private bool isSleeping = false;
    public static bool canWalk = false;

    [Header("Buttons")]
    public GameObject[] buttonsToDisable;

    public Animator foodAnimator;
    public Animator FaucerAnimator;

    [Header("Bath Settings")]
    public GameObject[] bubblePrefabs;
    public Animator ShowerAnimator;
    public GameObject ShowerButton;
    public GameObject SoapQuantity;
    public GameObject SoapNotEnoughPanel;
    public TMP_Text SoapQuantityText;
    public Sprite[] soaps = new Sprite[4];
    public SpriteRenderer soap;
    private bool isTouching = false;
    private static int bubbleCount = 0;
    private float bubbleFlowDuration = 10f;
    private float bubbleFadeDuration = 7f;

    [Header("Sleep Settings")]
    public GameObject lightEffect;
    private bool isLightOn = false;
    private bool FTLIFS = false;
    public StatManager statManager;

    [Header("Accessory Settings")]
    public GameObject[] accessoryObjects;
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

    // ✅ ADD INITIALIZATION FLAG
    private bool isInitialized = false;

    void Start()
    {
        try
        {
            Debug.Log("🐾 PetBehaviour.Start() - Beginning");

            // ✅ Validate critical references first
            if (!ValidateCriticalReferences())
            {
                Debug.LogError("❌ PetBehaviour initialization failed - missing critical references!");
                return;
            }

            animator = GetComponent<Animator>();
            pet = new Pet();

            SetInitialPosition();

            // ✅ Safe accessory initialization with null checks
            if (accessoryObjects != null && accessoryObjects.Length > 2 && accessoryObjects[2] != null)
            {
                glassesOriginalPosition = accessoryObjects[2].transform.localPosition;
                glassesOriginalScale = accessoryObjects[2].transform.localScale;
            }

            if (PlayerPrefs.GetInt(PlayerPrefKeys.isSleeping, 0) == 1)
            {
                ToggleLightEffect();
            } 
            else
            {
                FTLIFS = true;
            }

            // ✅ Wait for HPS to be ready before calling reflectPetData
            StartCoroutine(DelayedReflectPetData());

            Debug.Log("✅ PetBehaviour.Start() - Completed");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Exception in PetBehaviour.Start(): {ex.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}");
        }
    }

    // ✅ NEW: Validate critical references
    private bool ValidateCriticalReferences()
    {
        bool isValid = true;

        if (HPS == null)
        {
            Debug.LogError("❌ HorizontalPageScroller (HPS) is not assigned!");
            isValid = false;
        }

        if (petNameTxt == null)
        {
            Debug.LogWarning("⚠️ petNameTxt is not assigned!");
        }

        if (foodStack == null)
        {
            Debug.LogWarning("⚠️ foodStack is not assigned!");
        }

        if (Coins == null || Coins.Length == 0)
        {
            Debug.LogWarning("⚠️ Coins array is empty!");
        }

        return isValid;
    }

    // ✅ IMPROVED: Wait for HPS to be fully ready
    private IEnumerator DelayedReflectPetData()
    {
        Debug.Log("⏳ Waiting for HorizontalPageScroller to initialize...");

        // Wait until HPS exists and is active
        float timeout = 5f;
        float elapsed = 0f;

        while (HPS == null && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        if (HPS == null)
        {
            Debug.LogError("❌ HorizontalPageScroller not found after timeout!");
            yield break;
        }

        // Wait for HPS to complete its Start() method
        yield return new WaitForSeconds(0.5f);

        Debug.Log("🔍 Calling reflectPetData after delay");

        try
        {
            reflectPetData();
            isInitialized = true;
            Debug.Log("✅ PetBehaviour fully initialized");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Exception in reflectPetData(): {ex.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}");
        }
    }

    public void reflectPetData()
    {
        try
        {
            string petKey = PlayerPrefKeys.PetPrefix;

            // Retrieve data from PlayerPrefs
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

            // ✅ Safe animator controller assignment
            if (animator != null && controllers != null && controllers.Length > 0)
            {
                switch (pet.pet_type)
                {
                    case "cat_1": 
                        animator.runtimeAnimatorController = controllers[0]; 
                        break;
                    case "dog_1": 
                        if (controllers.Length > 3)
                            animator.runtimeAnimatorController = controllers[3]; 
                        break;
                    default: 
                        animator.runtimeAnimatorController = controllers[0]; 
                        break;
                }
            }

            // ✅ Safe UI updates with null checks
            if (petNameTxt != null)
                petNameTxt.text = pet.pet_name;

            if (foodStack != null)
                foodStack.text = pet.food_stack.ToString() + "x";

            if (Coins != null)
            {
                foreach (var coins in Coins)
                {
                    if (coins != null)
                        coins.text = pet.coins.ToString();
                }
            }

            // ✅ Safe HPS update with validation
            if (HPS != null)
            {
                int[] stats = new int[] { pet.playfulness, pet.hunger, pet.hygiene, pet.sleep };
                HPS.UpdateButtonFill(stats);
            }
            else
            {
                Debug.LogError("❌ HPS is null when trying to update button fills!");
            }

            // Set accessories based on saved data
            isHatOn = pet.pet_head > 0;
            isCollarOn = pet.pet_neck > 0;
            isGlassesOn = pet.pet_eyes > 0;

            // ✅ Safe accessory updates
            if (accessoryObjects != null && accessoryObjects.Length >= 3)
            {
                if (accessoryObjects[0] != null)
                    accessoryObjects[0].SetActive(isHatOn);
                if (accessoryObjects[1] != null)
                    accessoryObjects[1].SetActive(isCollarOn);
                if (accessoryObjects[2] != null)
                    accessoryObjects[2].SetActive(isGlassesOn);
            }
            
            // ✅ Safe sprite assignments with bounds checking
            if (accessoryObjects != null)
            {
                for (int i = 0; i < accessoryObjects.Length; i++)
                {
                    if (accessoryObjects[i] == null) continue;

                    SpriteRenderer sr = accessoryObjects[i].GetComponent<SpriteRenderer>();
                    if (sr == null) continue;

                    switch (i)
                    {
                        case 0: 
                            if (Hats != null && pet.pet_head - 1 >= 0 && pet.pet_head - 1 < Hats.Length)
                                sr.sprite = Hats[pet.pet_head - 1];
                            break;
                        case 1: 
                            if (Glasses != null && pet.pet_eyes - 5 >= 0 && pet.pet_eyes - 5 < Glasses.Length)
                                sr.sprite = Glasses[pet.pet_eyes - 5];
                            break;
                        case 2: 
                            if (Collars != null && pet.pet_neck - 9 >= 0 && pet.pet_neck - 9 < Collars.Length)
                                sr.sprite = Collars[pet.pet_neck - 9];
                            break;
                        case 3: 
                            if (SideCollars != null && pet.pet_neck - 9 >= 0 && pet.pet_neck - 9 < SideCollars.Length)
                                sr.sprite = SideCollars[pet.pet_neck - 9];
                            break;
                        case 4: 
                            if (SideCollars != null && pet.pet_neck - 5 >= 4 && pet.pet_neck - 5 < SideCollars.Length)
                                sr.sprite = SideCollars[pet.pet_neck - 5];
                            break;
                        case 5: 
                            if (SideGlasses != null && pet.pet_eyes - 5 >= 0 && pet.pet_eyes - 5 < SideGlasses.Length)
                                sr.sprite = SideGlasses[pet.pet_eyes - 5];
                            break;
                        case 6: 
                            if (SideGlasses != null && pet.pet_eyes - 1 >= 4 && pet.pet_eyes - 1 < SideGlasses.Length)
                                sr.sprite = SideGlasses[pet.pet_eyes - 1];
                            break;
                        case 7: 
                            if (Hats != null && pet.pet_head - 1 >= 0 && pet.pet_head - 1 < Hats.Length)
                                sr.sprite = Hats[pet.pet_head - 1];
                            break;
                        case 8: 
                            if (Hats != null && pet.pet_head - 1 >= 0 && pet.pet_head - 1 < Hats.Length)
                                sr.sprite = Hats[pet.pet_head - 1];
                            break;
                    }
                }
            }

            Debug.Log("✅ reflectPetData completed successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Exception in reflectPetData(): {ex.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}");
        }
    }

    private void SetInitialPosition()
    {
        transform.position = new Vector3(0f, -2.25f, 0f);
        transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        
        if (animator != null)
            animator.SetInteger("sleepType", 0);
    }

    void Update()
    {
        if (!isInitialized) return; // ✅ Don't run Update until fully initialized

        if (canWalk && !isSleeping)
        {
            HandleBehaviorTimers();
            AdjustScaleBasedOnPosition();
        }
    }

    // ... rest of the methods remain the same ...
    // (Keep all the other methods from your original PetBehaviour.cs)

    private void HandleBehaviorTimers()
    {
        if (timer <= 0f)
        {
            timer = Random.Range(changeDirectionTimeMin, changeDirectionTimeMax);

            int behaviorChoice = Random.Range(0, 100);

            if (behaviorChoice < 15)
            {
                isIdle = false;
                isMoving = false;
                StartSleep();
            }
            else if (behaviorChoice < 50)
            {
                isIdle = false;
                isMoving = true;
                StartWalk();
            }
            else
            {
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
        sleepState = 0;
        if (animator != null)
        {
            animator.SetBool("goLeft", false);
            animator.SetBool("goRight", false);
            animator.SetInteger("sleepType", sleepState);
        }
        isIdle = true;

        StartCoroutine(idleAccessories());
        transform.localScale = new Vector3(0.5f, 0.5f, 1f);
    }

    private IEnumerator idleAccessories()
    {
        yield return new WaitForSeconds(.1f);
        
        if (accessoryObjects == null) yield break;

        for (int i = 0; i < accessoryObjects.Length; i++)
        {
            if (accessoryObjects[i] == null) continue;

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
        
        if (accessoryObjects == null) yield break;

        for (int i = 0; i < accessoryObjects.Length; i++)
        {
            if (accessoryObjects[i] == null) continue;

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
        sleepState = 0;
        if (animator != null)
        {
            animator.SetBool("goLeft", false);
            animator.SetBool("goRight", false);
            animator.SetInteger("sleepType", sleepState);
        }

        float targetX = Random.Range(-1.91f, 1.91f);
        if (targetX < transform.position.x)
        {
            goLeft = true;
            goRight = false;
        }
        else
        {
            goLeft = false;
            goRight = true;
        }

        StartCoroutine(walkingAccessories(goRight));

        if (animator != null)
        {
            animator.SetBool("goLeft", goLeft);
            animator.SetBool("goRight", goRight);
        }

        StartCoroutine(WalkToRandomPosition(goLeft));
    }

    private void StartSleep()
    {
        isSleeping = true;

        int sleepChoice = Random.Range(3, 5);
        sleepState = sleepChoice;
        
        if (animator != null)
            animator.SetInteger("sleepType", sleepState);
            
        if (accessoryObjects != null)
        {
            foreach (var accessory in accessoryObjects)
            {
                if (accessory != null)
                    accessory.SetActive(false);
            }
        }

        float sleepDuration = Random.Range(layTimeMin, layTimeMax);
        StartCoroutine(LayForDuration(sleepDuration));
    }

    private IEnumerator WalkToRandomPosition(bool isWalkingLeft)
    {
        float targetX = Random.Range(-1.91f, 1.91f);
        float targetY = Random.Range(-2.55f, -1.88f);

        float elapsedTime = 0f;
        Vector3 startingPosition = transform.position;
        Vector3 targetPosition = new Vector3(targetX, targetY, 0f);

        while (elapsedTime < 1f)
        {
            float distanceCovered = (elapsedTime / 1f) * moveSpeed;
            transform.position = Vector3.Lerp(startingPosition, targetPosition, distanceCovered);
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        isMoving = false;
        StartIdle();
    }

    private IEnumerator LayForDuration(float duration)
    {
        yield return new WaitForSeconds(duration);

        isSleeping = false;
        StartIdle();
    }

    private void AdjustScaleBasedOnPosition()
    {
        float currentY = transform.position.y;
        float normalizedY = Mathf.InverseLerp(-2.55f, -1.88f, currentY);

        float idleScale = Mathf.Lerp(0.5f, 0.15f, normalizedY);
        float movementScale = Mathf.Lerp(0.5f, 0.2f, normalizedY);

        if (isIdle)
        {
            transform.localScale = new Vector3(idleScale, idleScale, 1f);
        }
        else if (isMoving || isSleeping)
        {
            transform.localScale = new Vector3(movementScale, movementScale, 1f);
        }
    }

    public void DisableBehavior()
    {
        canWalk = false;
        isMoving = false;
        isIdle = true;

        if (animator != null)
        {
            animator.SetBool("goLeft", false);
            animator.SetBool("goRight", false);
            animator.SetInteger("sleepType", 0);
        }
    }

    public IEnumerator WalkToInitialPosition()
    {
        Vector3 initialPosition = new Vector3(0f, -2.25f, 0f);

        if (animator != null)
        {
            animator.SetBool("goLeft", initialPosition.x < transform.position.x);
            animator.SetBool("goRight", initialPosition.x > transform.position.x);
        }

        float moveDuration = 2f;
        float elapsedTime = 0f;
        Vector3 startingPosition = transform.position;

        while (elapsedTime < moveDuration)
        {
            transform.position = Vector3.Lerp(startingPosition, initialPosition, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = initialPosition;
        StartIdle();
    }

    // ✅ ADD TRY-CATCH to all public methods that might be called externally
    public void OnFeedButtonPressed()
    {
        try
        {
            if (foodAnimator != null)
            {
                foodAnimator.SetTrigger("GoEat");
            }
            
            if (animator != null)
            {
                animator.SetBool("eat", true);
            }
            
            transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            
            if (accessoryObjects != null)
            {
                for (int i = 0; i < accessoryObjects.Length; i++)
                {
                    if (accessoryObjects[i] == null) continue;

                    if (i == 2)
                    {
                        accessoryObjects[i].transform.localScale = new Vector3(
                            accessoryObjects[i].transform.localScale.x * 0.5f, 
                            accessoryObjects[i].transform.localScale.y * 0.5f, 
                            1f
                        );
                        accessoryObjects[i].transform.localPosition = new Vector3(-0.25f, 0.025f, 1f);
                        continue;
                    }
                    accessoryObjects[i].SetActive(false);
                }
            }

            if (SM != null)
            {
                SM.PlayEatingSoundForDuration(8f);
            }

            if (buttonsToDisable != null)
            {
                foreach (var button in buttonsToDisable)
                {
                    if (button != null)
                        button.SetActive(false);
                }
            }

            StartCoroutine(ReenableButtonsAfterDelay(10f));
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Exception in OnFeedButtonPressed(): {ex.Message}");
        }
    }

    private void OnFinishFeeding()
    {
        try
        {
            if (foodAnimator != null)
            {
                foodAnimator.ResetTrigger("GoEat");
            }
            
            if (animator != null)
            {
                animator.SetBool("eat", false);
            }
            
            transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            
            if (accessoryObjects != null)
            {
                for (int i = 0; i < accessoryObjects.Length; i++)
                {
                    if (accessoryObjects[i] == null) continue;

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
                
                if (accessoryObjects.Length > 2 && accessoryObjects[2] != null)
                {
                    accessoryObjects[2].transform.localPosition = glassesOriginalPosition;
                    accessoryObjects[2].transform.localScale = glassesOriginalScale;
                }
            }

            int playfulness = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetPlayfulness);
            int hunger = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHunger) + 10;
            if (hunger >= 100)
            {
                hunger = 100;
            }
            int bath = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHygiene);
            int sleep = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetSleep);

            if (StorageBridge.Instance != null)
            {
                StorageBridge.Instance.SaveValue(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHunger, hunger);
            }

            int[] stats = new int[] { playfulness, hunger, bath, sleep };
        
            if (HPS != null)
            {
                HPS.UpdateButtonFill(stats);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Exception in OnFinishFeeding(): {ex.Message}");
        }
    }

    private IEnumerator ReenableButtonsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (buttonsToDisable != null)
        {
            foreach (var button in buttonsToDisable)
            {
                if (button != null)
                    button.SetActive(true);
            }
        }
        
        OnFinishFeeding();
    }

    // ... Include ALL remaining methods from your original PetBehaviour.cs ...
    // (OnCollisionEnter2D, OnCollisionExit2D, ContinuouslySpawnBubbles, etc.)
    // I'm truncating here due to length, but add them all with try-catch blocks

    public void ToggleLightEffect()
    {
        try
        {
            isLightOn = !isLightOn;
            if (lightEffect != null)
                lightEffect.SetActive(isLightOn);
                
            if (buttonsToDisable != null)
            {
                foreach (var button in buttonsToDisable)
                {
                    if (button != null)
                        button.SetActive(!isLightOn);
                }
            }
            
            if (SM != null)
                SM.PlayLightSound();
                
            if (isLightOn)
            {
                if (statManager != null)
                    statManager.isSleeping = true;
                    
                int sleepChoice = Random.Range(1, 5);
                sleepState = sleepChoice;
                transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                
                if (animator != null)
                    animator.SetInteger("sleepType", sleepState);
                    
                if (accessoryObjects != null)
                {
                    foreach (var accessory in accessoryObjects)
                    {
                        if (accessory != null)
                            accessory.SetActive(false);
                    }
                }
                
                isSleeping = true;
                canWalk = false;
                
                if (StorageBridge.Instance != null)
                    StorageBridge.Instance.SaveValue(PlayerPrefKeys.isSleeping, isSleeping ? 1 : 0);
            }
            else
            {
                if (statManager != null)
                    statManager.isSleeping = false;
                    
                isSleeping = false;
                
                if (buttonsToDisable != null)
                {
                    foreach (var button in buttonsToDisable)
                    {
                        if (button != null)
                            button.SetActive(true);
                    }
                }
                
                if (StorageBridge.Instance != null)
                    StorageBridge.Instance.SaveValue(PlayerPrefKeys.isSleeping, isSleeping ? 1 : 0);
                    
                StartIdle();
            }
            
            if (!FTLIFS)
            {
                FTLIFS = true;
            }
            else
            {
                StartCoroutine(toggleSleepOnBackend(isSleeping));
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Exception in ToggleLightEffect(): {ex.Message}");
        }
    }

    private IEnumerator toggleSleepOnBackend(bool isSleeping)
    {
        // ✅ NO try-catch when using yield return
        string petKey = PlayerPrefKeys.PetPrefix;
        int petID = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);

        string apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL);
        string url = $"{apiUrl}/toggle-pet-sleep";

        SleepPayload jsonPayload = new SleepPayload
        {
            petId = petID,
            isSleep = isSleeping
        };

        string jsonString = JsonUtility.ToJson(jsonPayload);
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] jsonToSend = System.Text.Encoding.UTF8.GetBytes(jsonString);
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        // Log result after yield (outside any try-catch)
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Error toggling sleep: {request.error}");
        }
    }

    public void FaucetFlow()
    {
        try
        {
            if (FaucerAnimator != null)
            {
                FaucerAnimator.SetBool("flow", true);
            }
            
            if (SM != null)
            {
                SM.PlayFaucet();
            }
            
            StartCoroutine(waitForFaucet());
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Exception in FaucetFlow(): {ex.Message}");
        }
    }
    
    private IEnumerator waitForFaucet()
    {
        yield return new WaitForSeconds(12f);
        
        if (FaucerAnimator != null)
        {
            FaucerAnimator.SetBool("flow", false);
        }
    }

    // Bath/Collision methods
    private void OnCollisionEnter2D(Collision2D collision)
    {
        try
        {
            if (collision.gameObject.CompareTag("soap"))
            {
                if (accessoryObjects != null)
                {
                    foreach (var acc in accessoryObjects) 
                    {
                        if (acc != null)
                            acc.SetActive(false);
                    }
                }
                
                if (buttonsToDisable != null)
                {
                    foreach (var button in buttonsToDisable)
                    {
                        if (button != null)
                            button.SetActive(false);
                    }
                }

                if (!isTouching)
                {
                    int currentCoins = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetCoins);
                    if (currentCoins <= 2)
                    {
                        if (SoapNotEnoughPanel != null)
                            SoapNotEnoughPanel.SetActive(true);
                            
                        if (accessoryObjects != null)
                        {
                            for (int i = 0; i < accessoryObjects.Length; i++)
                            {
                                if (accessoryObjects[i] == null) continue;

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

                        if (buttonsToDisable != null)
                        {
                            foreach (var button in buttonsToDisable)
                            {
                                if (button != null)
                                    button.SetActive(true);
                            }
                        }
                        return;
                    }

                    isTouching = true;
                    
                    if (SoapQuantity != null)
                        SoapQuantity.SetActive(false);
                        
                    StartCoroutine(ContinuouslySpawnBubbles());
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Exception in OnCollisionEnter2D(): {ex.Message}");
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        try
        {
            if (collision.gameObject.CompareTag("soap"))
            {
                isTouching = false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Exception in OnCollisionExit2D(): {ex.Message}");
        }
    }

    private IEnumerator ContinuouslySpawnBubbles()
    {
        // ✅ NO try-catch when using yield return - use null checks instead
        Collider2D petCollider = GetComponent<Collider2D>();
        if (petCollider == null)
        {
            Debug.LogError("❌ No collider found on pet!");
            yield break;
        }

        Bounds colliderBounds = petCollider.bounds;

        while (isTouching && bubbleCount < 10)
        {
            bubbleCount++;
            
            if (bubbleCount % 2 == 1 && SM != null)
            {
                SM.PlayBubble();
            }

            float randomX = Random.Range(colliderBounds.min.x, colliderBounds.max.x);
            float randomY = Random.Range(colliderBounds.min.y, colliderBounds.max.y);
            float randomX2 = Random.Range(colliderBounds.min.x, colliderBounds.max.x);
            float randomY2 = Random.Range(colliderBounds.min.y, colliderBounds.max.y);

            Vector2 spawnPosition = new Vector2(randomX, randomY);
            Vector2 spawnPosition2 = new Vector2(randomX2, randomY2);

            if (bubblePrefabs != null && bubblePrefabs.Length > 0)
            {
                GameObject bubble1 = bubblePrefabs[Random.Range(0, bubblePrefabs.Length)];
                GameObject bubble2 = bubblePrefabs[Random.Range(0, bubblePrefabs.Length)];
                
                if (bubble1 != null)
                    Instantiate(bubble1, spawnPosition, Quaternion.identity);
                if (bubble2 != null)
                    Instantiate(bubble2, spawnPosition2, Quaternion.identity);
            }

            yield return new WaitForSeconds(0.2f);
        }

        if (bubbleCount >= 10 && ShowerButton != null)
        {
            ShowerButton.SetActive(true);
        }

        isTouching = false;
    }

    public void OpenShower()
    {
        StartCoroutine(ShowerRoutine());
    }

    private IEnumerator ShowerRoutine()
    {
        // ✅ NO try-catch when using yield return
        if (ShowerAnimator != null)
        {
            ShowerAnimator.SetTrigger("ShowerOn");
        }
        
        if (SM != null)
        {
            SM.PlayShowerSoundForDuration(8f);
        }
        
        MoveAndFadeBubbles();
    
        yield return new WaitForSeconds(10f);
    
        if (ShowerAnimator != null)
        {
            ShowerAnimator.ResetTrigger("ShowerOn");
        }
        
        yield return new WaitForSeconds(1f);
    
        if (accessoryObjects != null)
        {
            for (int i = 0; i < accessoryObjects.Length; i++)
            {
                if (accessoryObjects[i] == null) continue;

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
        
        if (buttonsToDisable != null)
        {
            foreach (var button in buttonsToDisable)
            {
                if (button != null)
                    button.SetActive(true);
            }
        }
        
        yield return StartCoroutine(UpdateSoapUsageOnBackend());

        if (DataManager.Instance != null)
        {
            DataManager.Instance.StartCoroutine(DataManager.Instance.HandlePetDataRequest());
        }
        
        if (ShowerButton != null)
            ShowerButton.SetActive(false);
            
        bubbleCount = 0;
    }
    
    private IEnumerator UpdateSoapUsageOnBackend()
    {
        string petKey = PlayerPrefKeys.PetPrefix;
        string apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL);
        string apiUrlSecondary = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary);
        int petId = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);

        string url = $"{apiUrlSecondary}/pets/{petId}/soapuse";

        var payload = new object();
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
            int currentCoins = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetCoins);
            
            if (StorageBridge.Instance != null)
            {
                StorageBridge.Instance.SaveValue(petKey + PlayerPrefKeys.PetCoins, currentCoins - 3);
            }

            if (Coins != null)
            {
                foreach (var coin in Coins)
                {
                    if (coin != null)
                        coin.text = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetCoins).ToString();
                }
            }

            int playfulness = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetPlayfulness);
            int hunger = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHunger);
            int bath = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHygiene) + 10;
            int sleep = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetSleep);
            
            if (StorageBridge.Instance != null)
            {
                StorageBridge.Instance.SaveValue(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHygiene, bath);
            }
            
            int[] stats = new int[] { playfulness, hunger, bath, sleep };

            if (HPS != null)
            {
                HPS.UpdateButtonFill(stats);
            }
        }
        else
        {
            Debug.LogError("Error updating soap usage on backend: " + request.error);
        }
        
        if (SoapQuantity != null)
            SoapQuantity.SetActive(true);
    }
 
    private void MoveAndFadeBubbles()
    {
        GameObject[] existingBubbles = GameObject.FindGameObjectsWithTag("Bubbles");

        foreach (var bubble in existingBubbles)
        {
            if (bubble != null)
                StartCoroutine(MoveAndFadeBubble(bubble));
        }
    }

    private IEnumerator MoveAndFadeBubble(GameObject bubble)
    {
        if (bubble == null) yield break;

        SpriteRenderer bubbleRenderer = bubble.GetComponent<SpriteRenderer>();
        if (bubbleRenderer == null) yield break;

        Vector3 startPosition = bubble.transform.position;
        Vector3 targetPosition = new Vector3(bubble.transform.position.x, -3.5f, bubble.transform.position.z);

        float moveDuration = bubbleFlowDuration;
        float fadeDuration = bubbleFadeDuration;

        float moveElapsedTime = 0f;
        float fadeElapsedTime = 0f;

        while (moveElapsedTime < moveDuration && bubble != null)
        {
            bubble.transform.position = Vector3.Lerp(startPosition, targetPosition, moveElapsedTime / moveDuration);

            float fadeProgress = fadeElapsedTime / fadeDuration;
            bubbleRenderer.color = new Color(bubbleRenderer.color.r, bubbleRenderer.color.g, bubbleRenderer.color.b, Mathf.Lerp(1f, 0f, fadeProgress));

            moveElapsedTime += Time.deltaTime;
            fadeElapsedTime += Time.deltaTime;

            yield return null;
        }

        if (bubble != null)
        {
            bubble.transform.position = targetPosition;
            bubbleRenderer.color = new Color(bubbleRenderer.color.r, bubbleRenderer.color.g, bubbleRenderer.color.b, 0f);
            Destroy(bubble);
        }
    }
}

[System.Serializable]
public class SleepPayload
{
    public int petId;
    public bool isSleep;
}