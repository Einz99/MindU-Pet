using UnityEngine;
using System.Collections;

public class HorizontalPageScroller : MonoBehaviour
{
    public static HorizontalPageScroller Instance { get; private set; }
    [Header("Scroll Settings")]
    public Transform contentParent; // Parent object containing all 4 sprites
    public float pageWidth = 1920f; // Width of each page (adjust to your screen/canvas size)
    public float scrollDuration = 1f; // Time to scroll between pages
    public AnimationCurve scrollCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Page Settings")]
    public int totalPages = 4; // Number of sprite pages
    public GameObject Shop;
    private int currentPage = 3;
    private bool isScrolling = false;
    private Vector3 targetPosition;
    private Vector3 startPosition;

    [Header("Selected Settings")]
    public GameObject[] unselectedObjects; // Array of unselected GameObjects
    public GameObject[] selectedObjects; // Array of selected GameObjects
    public Animator animator; // Reference to the Animator component

    [Header("Pet Settings")]
    public GameObject petObject; // Reference to the pet GameObject
    private PetBehaviour petBehaviour; // Reference to the PetBehaviour script
    public GameObject soapObject; // Reference to the soap GameObject
    public GameObject SoapQuantity;
    public GameObject lightButtonObject; // Reference to the light button GameObject
    public GameObject FoodQuantity;

    [Header("Curtain Settings")]
    public GameObject curtain;
    public GameObject curtainButton;
    private bool isCurtainOpen;

    [Header("Accessory Settings")]
    public GameObject[] accessoryObjects; // Array of accessory GameObjects to toggle
    private bool isHatOn = false;
    private bool isCollarOn = false;
    private bool isGlassesOn = false;

    [Header("Stats")]
    public ButtonFill[] ButtonFills = new ButtonFill[4];

    void Start()
    {
        isCurtainOpen = PlayerPrefs.GetInt(PlayerPrefKeys.isCurtainOpen, 0) == 1;
        petBehaviour = petObject.GetComponent<PetBehaviour>();
        // Set initial position
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

    }

    private void ScrollToPage(int pageIndex)
    {
        if (contentParent == null || isScrolling) return; // Prevent scrolling while animating

        // Calculate target position (moving left means negative X)
        float targetX = -pageIndex * pageWidth;
        targetPosition = new Vector3(targetX, contentParent.localPosition.y, contentParent.localPosition.z);
        startPosition = contentParent.localPosition;
        StartCoroutine(AnimateScroll());

        // Update selected/unselected objects
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
                // Set the curtain active based on isCurtainOpen when on page 0
                curtain.SetActive(isCurtainOpen);
                curtainButton.SetActive(true);
                lightButtonObject.SetActive(false);
                soapSpriteRenderer.enabled = false;
                SoapQuantity.SetActive(false);
                FoodQuantity.SetActive(false);
                break;

            case 1:
                curtain.SetActive(false);
                curtainButton.SetActive(false);
                lightButtonObject.SetActive(false);
                soapSpriteRenderer.enabled = false;
                SoapQuantity.SetActive(false);
                StartCoroutine(ShowFoodQuantity());
                break;

            case 2:
                // Enable the soap sprite renderer on page 2
                curtain.SetActive(false);
                curtainButton.SetActive(false);
                lightButtonObject.SetActive(false);
                StartCoroutine(ShowSoap());
                FoodQuantity.SetActive(false);
                break;

            case 3:
                // Enable the light button on page 3
                lightButtonObject.SetActive(true);
                curtain.SetActive(false);
                curtainButton.SetActive(false);
                soapSpriteRenderer.enabled = false;
                SoapQuantity.SetActive(false);
                FoodQuantity.SetActive(false);
                break;
        }
    }

    private IEnumerator ShowSoap()
    {
        yield return new WaitForSeconds(0.8f);
        SpriteRenderer soapSpriteRenderer = soapObject.GetComponent<SpriteRenderer>();
        soapSpriteRenderer.enabled = true;
        SoapQuantity.SetActive(true);

    }
    private IEnumerator ShowFoodQuantity()
    {
        yield return new WaitForSeconds(0.8f);
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

            // Interpolate position
            contentParent.localPosition = Vector3.Lerp(startPosition, targetPosition, curveValue);
            yield return null;
        }

        // Ensure final position is exact
        contentParent.localPosition = targetPosition;

        // After scrolling, reset animator parameters
        animator.SetBool("goLeft", false);
        animator.SetBool("goRight", false);

        StartCoroutine(idleAccessories());

        if (currentPage == 0)
        {
            PetBehaviour.canWalk = true; // Allow pet to walk after scrolling
        }
        isScrolling = false; // Allow new scrolling after animation finishes
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

    // Public method to jump to specific page
    public void JumpToPage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= totalPages || isScrolling) return; // Prevent jumping if still scrolling

        if (currentPage == 0)
        {
            petBehaviour.DisableBehavior(); // Disable pet behavior when leaving page 0
            StartCoroutine(petBehaviour.WalkToInitialPosition());
        }

        bool goLeft = currentPage > pageIndex;

        for (int i = 0; i < accessoryObjects.Length; i++)
        {
            accessoryObjects[i].SetActive(false);
            if (!goLeft && ((isCollarOn && i == 3) || (isGlassesOn && i == 5)))
            {
                accessoryObjects[i].SetActive(true);
                continue;
            }
            else if (goLeft && ((isCollarOn && i == 4) || (isGlassesOn && i == 6)))
            {
                accessoryObjects[i].SetActive(true);
                continue;
            }
            else if (i == 0 && isHatOn)
            {
                accessoryObjects[i].SetActive(true);
                continue;
            }
            accessoryObjects[i].SetActive(false);
        }


        if (goLeft)
        {
            animator.SetBool("goLeft", true); // Going left
            animator.SetBool("goRight", false); // Set goRight to false
        }
        else 
        {
            animator.SetBool("goLeft", false); // Set goLeft to false
            animator.SetBool("goRight", true); // Going right
        }
        currentPage = pageIndex;
        ScrollToPage(currentPage);
    }
    

    // Getter for current page
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

    public void UpdateButtonFill(int[] newValues)
    {
        if (newValues.Length != ButtonFills.Length)
        {
            Debug.LogError("The number of values in newValues array must match the ButtonFills array length!");
            return;
        }

        // Loop through all ButtonFills and set their fill percentage
        for (int i = 0; i < ButtonFills.Length; i++)
        {
            ButtonFills[i].SetFillPercentage(newValues[i]);
        }
    }
}
