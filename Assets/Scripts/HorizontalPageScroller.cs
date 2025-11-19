using UnityEngine;
using System.Collections;
using System;

public class HorizontalPageScroller : MonoBehaviour
{
    [Header("Scroll Settings")]
    public Transform contentParent;
    public float pageWidth = 1920f;
    public float scrollDuration = 1f;
    public AnimationCurve scrollCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public GameObject DailyReward;

    [Header("Page Settings")]
    public int totalPages = 4;
    public GameObject Shop;
    private int currentPage = 3;
    private bool isScrolling = false;
    private Vector3 targetPosition;
    private Vector3 startPosition;

    [Header("Selected Settings")]
    public GameObject[] unselectedObjects;
    public GameObject[] selectedObjects;
    public Animator animator;

    [Header("Pet Settings")]
    public GameObject petObject;
    private PetBehaviour petBehaviour;
    public GameObject soapObject;
    public GameObject SoapQuantity;
    public GameObject lightButtonObject;
    public GameObject FoodQuantity;
    public GameObject Faucet;
    public GameObject Toys;

    [Header("Curtain Settings")]
    public GameObject curtain;
    public GameObject curtainButton;
    private bool isCurtainOpen;

    [Header("Accessory Settings")]
    public GameObject[] accessoryObjects;
    private bool isHatOn = false;
    private bool isCollarOn = false;
    private bool isGlassesOn = false;

    [Header("Stats")]
    public ButtonFill[] ButtonFills = new ButtonFill[4];

    private const string LAST_DAILY_REWARD_KEY = "LastDailyRewardDate";
    
    // ✅ ADD THIS FLAG
    private bool isInitialized = false;
    
    void Start()
    {
        // ✅ VALIDATE ButtonFills array FIRST
        ValidateButtonFills();
        
        CheckAndShowDailyRewards();

        isCurtainOpen = PlayerPrefs.GetInt(PlayerPrefKeys.isCurtainOpen, 0) == 1;
        petBehaviour = petObject.GetComponent<PetBehaviour>();
        
        if (contentParent != null)
        {
            contentParent.localPosition = new Vector3(0f, contentParent.localPosition.y, contentParent.localPosition.z);
        }
        
        scrollDuration = 0f;
        JumpToPage(currentPage);
        scrollDuration = 1f;
        curtain.SetActive(isCurtainOpen);

        string petKey = PlayerPrefKeys.PetPrefix;

        isHatOn = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHead, 0) > 0;
        isCollarOn = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetNeck, 0) > 0;
        isGlassesOn = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetEyes, 0) > 0;

        // ✅ MARK AS INITIALIZED
        isInitialized = true;
        Debug.Log("✅ HorizontalPageScroller initialized successfully");
    }

    // ✅ NEW METHOD: Validate ButtonFills array
    private void ValidateButtonFills()
    {
        if (ButtonFills == null || ButtonFills.Length != 4)
        {
            Debug.LogError("❌ ButtonFills array is null or wrong size! Expected 4 elements.");
            ButtonFills = new ButtonFill[4];
            return;
        }

        int nullCount = 0;
        for (int i = 0; i < ButtonFills.Length; i++)
        {
            if (ButtonFills[i] == null)
            {
                Debug.LogError($"❌ ButtonFills[{i}] is null!");
                nullCount++;
            }
        }

        if (nullCount > 0)
        {
            Debug.LogError($"❌ Found {nullCount} null ButtonFill references! Please assign them in Inspector.");
        }
        else
        {
            Debug.Log("✅ All ButtonFills validated successfully");
        }
    }

    private void CheckAndShowDailyRewards()
    {
        string todayDate = DateTime.Now.ToString("yyyy-MM-dd");
        string lastRewardDate = PlayerPrefs.GetString(LAST_DAILY_REWARD_KEY, "");

        if (string.IsNullOrEmpty(lastRewardDate) || lastRewardDate != todayDate)
        {
            if (DailyReward != null)
            {
                DailyReward.SetActive(true);
            }
        }
        else
        {
            if (DailyReward != null)
            {
                DailyReward.SetActive(false);
            }
        }
    }

    private void ScrollToPage(int pageIndex)
    {
        if (contentParent == null || isScrolling) return;

        float targetX = -pageIndex * pageWidth;
        targetPosition = new Vector3(targetX, contentParent.localPosition.y, contentParent.localPosition.z);
        startPosition = contentParent.localPosition;
        StartCoroutine(AnimateScroll());

        for (int i = 0; i < totalPages; i++)
        {
            if (i == pageIndex)
            {
                if (i < selectedObjects.Length && selectedObjects[i] != null) selectedObjects[i].SetActive(true);
                if (i < unselectedObjects.Length && unselectedObjects[i] != null) unselectedObjects[i].SetActive(false);
            }
            else
            {
                if (i < selectedObjects.Length && selectedObjects[i] != null) selectedObjects[i].SetActive(false);
                if (i < unselectedObjects.Length && unselectedObjects[i] != null) unselectedObjects[i].SetActive(true);
            }
        }

        SpriteRenderer soapSpriteRenderer = soapObject.GetComponent<SpriteRenderer>();
        switch (pageIndex)
        {
            case 0:
                curtain.SetActive(isCurtainOpen);
                curtainButton.SetActive(true);
                lightButtonObject.SetActive(false);
                soapSpriteRenderer.enabled = false;
                Faucet.SetActive(false);
                SoapQuantity.SetActive(false);
                FoodQuantity.SetActive(false);
                StartCoroutine(ShowToys());
                break;

            case 1:
                curtain.SetActive(false);
                curtainButton.SetActive(false);
                lightButtonObject.SetActive(false);
                Faucet.SetActive(true);
                soapSpriteRenderer.enabled = false;
                Toys.SetActive(false);
                SoapQuantity.SetActive(false);
                StartCoroutine(ShowFoodQuantity());
                break;

            case 2:
                curtain.SetActive(false);
                curtainButton.SetActive(false);
                lightButtonObject.SetActive(false);
                Toys.SetActive(false);
                Faucet.SetActive(false);
                StartCoroutine(ShowSoap());
                FoodQuantity.SetActive(false);
                break;

            case 3:
                lightButtonObject.SetActive(true);
                Toys.SetActive(false);
                curtain.SetActive(false);
                curtainButton.SetActive(false);
                Faucet.SetActive(false);
                soapSpriteRenderer.enabled = false;
                SoapQuantity.SetActive(false);
                FoodQuantity.SetActive(false);
                break;
        }
    }

    private IEnumerator ShowToys()
    {
        yield return new WaitForSeconds(0.8f + scrollDuration - 1f);
        Toys.SetActive(true);
    }

    private IEnumerator ShowSoap()
    {
        yield return new WaitForSeconds(0.8f + scrollDuration - 1f);
        SpriteRenderer soapSpriteRenderer = soapObject.GetComponent<SpriteRenderer>();
        soapSpriteRenderer.enabled = true;
        SoapQuantity.SetActive(true);
    }

    private IEnumerator ShowFoodQuantity()
    {
        yield return new WaitForSeconds(0.8f + scrollDuration - 1f);
        FoodQuantity.SetActive(true);
    }

    private IEnumerator AnimateScroll()
    {
        isScrolling = true;
        float elapsedTime = 0f;

        while (elapsedTime < scrollDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / scrollDuration;
            float curveValue = scrollCurve.Evaluate(progress);

            contentParent.localPosition = Vector3.Lerp(startPosition, targetPosition, curveValue);
            yield return null;
        }

        contentParent.localPosition = targetPosition;

        animator.SetBool("goLeft", false);
        animator.SetBool("goRight", false);

        StartCoroutine(idleAccessories());

        if (currentPage == 0)
        {
            PetBehaviour.canWalk = true;
        }
        isScrolling = false;
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

    public void JumpToPage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= totalPages || isScrolling) return;

        if (currentPage == 0)
        {
            petBehaviour.DisableBehavior();
            StartCoroutine(petBehaviour.WalkToInitialPosition());
        }

        int durationMultiplier = Math.Abs(currentPage - pageIndex);
        scrollDuration = 1f * durationMultiplier;

        bool goLeft = currentPage > pageIndex;

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

        if (goLeft)
        {
            animator.SetBool("goLeft", true);
            animator.SetBool("goRight", false);
        }
        else 
        {
            animator.SetBool("goLeft", false);
            animator.SetBool("goRight", true);
        }
        currentPage = pageIndex;
        ScrollToPage(currentPage);
    }

    public int GetCurrentPage()
    {
        return currentPage;
    }

    public void ToggleCurtain()
    {
        isCurtainOpen = !isCurtainOpen;
        curtain.SetActive(isCurtainOpen);
        PlayerPrefs.SetInt(PlayerPrefKeys.isCurtainOpen, isCurtainOpen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void OpenShop()
    {
        if (Shop != null)
        {
            Shop.SetActive(true);
        }
    }

    // ✅ IMPROVED: Add safety checks and early return if not initialized
    public void UpdateButtonFill(int[] newValues)
    {
        // ✅ CHECK 1: Verify initialization
        if (!isInitialized)
        {
            Debug.LogWarning("⚠️ UpdateButtonFill called before HorizontalPageScroller is initialized! Ignoring...");
            return;
        }

        // ✅ CHECK 2: Validate input array
        if (newValues == null)
        {
            Debug.LogError("❌ UpdateButtonFill received null array!");
            return;
        }

        // ✅ CHECK 3: Validate ButtonFills array
        if (ButtonFills == null)
        {
            Debug.LogError("❌ ButtonFills array is null!");
            return;
        }

        // ✅ CHECK 4: Validate array lengths match
        if (newValues.Length != ButtonFills.Length)
        {
            Debug.LogError($"❌ Array length mismatch! Expected {ButtonFills.Length}, got {newValues.Length}");
            return;
        }

        // ✅ UPDATE: With individual null checks
        for (int i = 0; i < ButtonFills.Length; i++)
        {
            if (ButtonFills[i] != null)
            {
                Debug.Log($"Setting ButtonFill[{i}] to {newValues[i]}%");
                ButtonFills[i].SetFillPercentage(newValues[i]);
            }
            else
            {
                Debug.LogError($"❌ ButtonFills[{i}] is null! Cannot update.");
            }
        }
    }
}