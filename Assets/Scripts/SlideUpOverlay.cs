using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;

public class SlideUpOverlay : MonoBehaviour
{
    public GameObject overlayPanel;  // Assign the Panel/Overlay in the Inspector
    public GameObject slidePanel;
    public GameObject confirmPanel;
    public TMP_InputField Input;
    public float slideSpeed = 1f;    // Adjust the speed of the slide
    private Vector3 targetPosition;  // The target position where the panel will slide to
    private Vector3 originalPosition;  // The initial position of the panel
    public GameObject animator;
    private int selected = 0;

    public TMP_Text PetName;
    public TMP_Text PetType;
    public Button Adopt;

    public RuntimeAnimatorController[] animatorControllers;

    void Start()
    {
        // Set the initial position of the panel (hidden off-screen at the bottom)
        originalPosition = slidePanel.GetComponent<RectTransform>().localPosition;
        targetPosition = new Vector3(originalPosition.x, -335, originalPosition.z); // Position the overlay at the screen's center
        // Initially set the overlay panel out of view (bottom of the screen)
        slidePanel.GetComponent<RectTransform>().localPosition = new Vector3(originalPosition.x, -Screen.height, originalPosition.z);
    }

    public void OnButtonClick(int index)  // This method will be called when the button is clicked
    {
        overlayPanel.SetActive(true);
        selected = index + 1;

        // Get the animator component
        Animator animatorComponent = animator.GetComponent<Animator>();

        // Set the appropriate Animator Controller based on the index
        animatorComponent.runtimeAnimatorController = animatorControllers[index];

        // Get the transform component of the object
        Transform transformComponent = animator.GetComponent<Transform>();

        // Scale based on the index (for index 1 and 2, scale up to 2200)
        if (index == 1 || index == 2)
        {
            transformComponent.localScale = new Vector3(2500f, 2500f, 1f);  // Scale both x and y to 2200
        }
        else
        {
            // Reset scale for other indices (e.g., 0)
            transformComponent.localScale = new Vector3(1300f, 1300f, 1f);  // Set scale to 1300
        }
        Debug.Log("Current Scale: " + transformComponent.localScale + "\nIndex: " + index);
        // Start the slide animation (you can also call this with a button event in Unity)
        StopAllCoroutines();  // Stop any previous slide coroutine
        StartCoroutine(SlideUpAnimation());
    }

    IEnumerator SlideUpAnimation()
    {
        float elapsedTime = 0f;
        Vector3 currentPos = slidePanel.GetComponent<RectTransform>().localPosition;

        // Slide the panel up to the target position
        while (elapsedTime < slideSpeed)
        {
            slidePanel.GetComponent<RectTransform>().localPosition = Vector3.Lerp(currentPos, targetPosition, (elapsedTime / slideSpeed));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Make sure the final position is exactly the target position
        slidePanel.GetComponent<RectTransform>().localPosition = targetPosition;
        Debug.Log(selected);
    }

    // Optional: To slide the panel down again (hide it)
    public void SlideDown()
    {
        selected = 0;
        Transform transformComponent = animator.GetComponent<Transform>();
        transformComponent.localScale = new Vector3(1300f, 1300f, 1f);

        StopAllCoroutines(); // Stop any previous slide coroutine
        StartCoroutine(SlideDownAnimation());
    }

    IEnumerator SlideDownAnimation()
    {
        float elapsedTime = 0f;
        Vector3 currentPos = slidePanel.GetComponent<RectTransform>().localPosition;
        Vector3 hidePosition = new Vector3(originalPosition.x, -Screen.height, originalPosition.z);  // Off-screen position

        while (elapsedTime < slideSpeed)
        {
            slidePanel.GetComponent<RectTransform>().localPosition = Vector3.Lerp(currentPos, hidePosition, (elapsedTime / slideSpeed));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Make sure the final position is exactly the hide position
        slidePanel.GetComponent<RectTransform>().localPosition = hidePosition;
        overlayPanel.SetActive(false);
    }

    public void OnConfirm()
    {
        if (string.IsNullOrEmpty(Input.text))
        {
            return;
        }

        // Set pet type based on the selected index
        switch (selected)
        {
            case 1: PetType.text = "Gray Cat"; break;
            case 2: PetType.text = "Calico"; break;
            case 3: PetType.text = "Tuxedo Cat"; break;
            case 4: PetType.text = "Gray Cat"; break;
            case 5: PetType.text = "Gray Cat"; break;
            case 6: PetType.text = "Gray Cat"; break;
            default: break;
        }

        // Set the pet name and button text
        PetName.text = Input.text;
        Adopt.GetComponentInChildren<TMP_Text>().text = $"Adopt\n{Input.text}";

        confirmPanel.SetActive(true);
    }

    // Create pet and send data to the server
    public void CreatePetAndSendData()
    {
        // Get the student_id from PlayerPrefs
        int studentId = PlayerPrefs.GetInt(PlayerPrefKeys.StudentID, 46); // Default to 46 if not found

        string petType = "";

        switch (selected) {
            case 1: petType = "cat_1"; break;
            case 2: petType = "cat_2"; break;
            case 3: petType = "cat_3"; break;
            case 4: petType = "dog_1"; break;
            case 5: petType = "dog_2"; break;
            case 6: petType = "dog_3"; break;
        }

        // Start the coroutine to send the pet data to the server
        StartCoroutine(SendPetDataToServer(PetName.text, petType, studentId));
    }

    private IEnumerator SendPetDataToServer(string petName, string petType, int studentId)
    {
        Debug.Log($"Sending data: student_id={studentId}, pet_name={petName}, pet_type={petType}");
        // Create the data object to send
        PetData petData = new PetData
        {
            pet_name = petName,
            pet_type = petType,
            student_id = studentId
        };

        string insertAPI = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary, "http://192.168.1.5:3000/api") + "/pets";
        Debug.Log(insertAPI);
        // Convert the data to JSON
        string jsonData = JsonUtility.ToJson(petData);
        UnityWebRequest request = new UnityWebRequest(insertAPI, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Pet successfully created: " + request.downloadHandler.text);

            DataManager.Instance.StartCoroutine(DataManager.Instance.HandlePetDataRequest());
            int playfulness = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetPlayfulness);
            int hunger = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHunger);
            int bath = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetHygiene); // Assuming bath is stored in hygiene
            int sleep = PlayerPrefs.GetInt(PlayerPrefKeys.PetPrefix + PlayerPrefKeys.PetSleep);

            // Create an array with the stats
            int[] stats = new int[] { playfulness, hunger, bath, sleep };
            // Call UpdateButtonFill with the stats array
            HorizontalPageScroller.Instance.UpdateButtonFill(stats);
        }
        else
        {
            Debug.LogError("Error creating pet: " + request.error);
        }
    }

    // Method to cancel the overlay and clear input
    public void OnCancelOverlay()
    {
        Input.text = "";
        PetName.text = "";
        overlayPanel.SetActive(false);
        SlideDown();
    }
}

// PetData class to hold the pet data for POST request
[System.Serializable]
public class PetData
{
    public string pet_name;
    public string pet_type;
    public int student_id;
}
