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
    public GameObject loadingScreen;
    public GameObject CreationScreen;
    public TMP_InputField Input;
    public float slideSpeed = 1f;    // Adjust the speed of the slide
    private Vector3 targetPosition;  // The target position where the panel will slide to
    private Vector3 originalPosition;  // The initial position of the panel
    public Animator animator;
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
        
        // Set the appropriate Animator Controller based on the index
        animator.runtimeAnimatorController = animatorControllers[index];
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
            case 1: PetType.text = "GRAY CAT"; break;
            case 2: PetType.text = "Calico"; break;
            case 3: PetType.text = "Tuxedo Cat"; break;
            case 4: PetType.text = "AKITA DOG"; break;
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

        switch (selected)
        {
            case 1: petType = "cat_1"; break;
            case 2: petType = "cat_2"; break;
            case 3: petType = "cat_3"; break;
            case 4: petType = "dog_1"; break;
            case 5: petType = "dog_2"; break;
            case 6: petType = "dog_3"; break;
        }
        loadingScreen.SetActive(true);
        CreationScreen.SetActive(false);
        overlayPanel.SetActive(false);
        confirmPanel.SetActive(false);

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

        string insertAPI = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary) + "/pets";
        Debug.Log($"InsertAPI From SlideUP: {insertAPI}");
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
