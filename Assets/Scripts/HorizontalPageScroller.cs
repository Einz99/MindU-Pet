using UnityEngine;
using System.Collections;

public class HorizontalPageScroller : MonoBehaviour
{
    [Header("Scroll Settings")]
    public Transform contentParent; // Parent object containing all 4 sprites
    public float pageWidth = 1920f; // Width of each page (adjust to your screen/canvas size)
    public float scrollDuration = 1f; // Time to scroll between pages
    public AnimationCurve scrollCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Page Settings")]
    public int totalPages = 4; // Number of sprite pages
    private int currentPage = 3;
    private bool isScrolling = false;
    private Vector3 targetPosition;
    private Vector3 startPosition;

    [Header("Selected Settings")]
    public GameObject[] unselectedObjects; // Array of unselected GameObjects
    public GameObject[] selectedObjects; // Array of selected GameObjects
    public Animator animator; // Reference to the Animator component

    void Start()
    {
        // Set initial position
        if (contentParent != null)
        {
            contentParent.localPosition = new Vector3(0f, contentParent.localPosition.y, contentParent.localPosition.z);
        }
        scrollDuration = 0f;
        JumpToPage(currentPage);
        scrollDuration = 1f;
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
        isScrolling = false; // Allow new scrolling after animation finishes
    }

    // Public method to jump to specific page
    public void JumpToPage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= totalPages || isScrolling) return; // Prevent jumping if still scrolling

        if (currentPage > pageIndex)
        {
            animator.SetBool("goLeft", true); // Going left
            animator.SetBool("goRight", false); // Set goRight to false
        }
        else if (currentPage < pageIndex)
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
}
