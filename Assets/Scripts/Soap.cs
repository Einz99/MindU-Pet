using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Soap : MonoBehaviour
{
    private InputAction clickAction;
    private bool isDragging = false;
    private Vector3 offset;
    private Rigidbody2D rb2d;
    public Sprite[] Soaps = new Sprite[4];
    public TMP_Text Soapquantity;

    private Vector3 originalPosition = new Vector3(-1.65f, 2.9f, 0);

    private void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        transform.position = originalPosition;  // Set the original position
        rb2d.bodyType = RigidbodyType2D.Kinematic;  // Start as kinematic

        EnablingActions();
    }

    private void OnDisable()
    {
        clickAction.Disable();
    }

    private void OnEnable()
    {
        EnablingActions();
        string petkey = PlayerPrefs.GetString(PlayerPrefKeys.PetPrefix);
        int soap_type = PlayerPrefs.GetInt(petkey + PlayerPrefKeys.soap_type);
        int soap_quantity = PlayerPrefs.GetInt(petkey + PlayerPrefKeys.soap_quantity);

        GetComponent<SpriteRenderer>().sprite = Soaps[soap_type];
        Soapquantity.text = soap_quantity + "x";
    }
    
    private void EnablingActions()
    {
        if (Application.isMobilePlatform)
        {
            clickAction = new InputAction(type: InputActionType.Button, binding: "<Touchscreen>/primaryTouch");
            clickAction.performed += OnTouchBegan;
            clickAction.canceled += OnTouchEnded;
            clickAction.Enable();
        }
        else
        {
            clickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
            clickAction.performed += OnTouchBegan;
            clickAction.canceled += OnTouchEnded;
            clickAction.Enable();
        }
    }
    

    private void OnTouchBegan(InputAction.CallbackContext context)
    {
        Vector2 inputPos;

        if (Application.isMobilePlatform)
        {
            inputPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else
        {
            inputPos = Mouse.current.position.ReadValue();
        }

        Vector3 worldTouchPos = Camera.main.ScreenToWorldPoint(inputPos);
        worldTouchPos.z = 0;

        RaycastHit2D hit = Physics2D.Raycast(worldTouchPos, Vector2.zero);

        if (hit.collider != null && hit.collider.gameObject == gameObject)
        {
            isDragging = true;
            offset = gameObject.transform.position - worldTouchPos;
            
            // Change Rigidbody2D to Dynamic for collisions
            rb2d.bodyType = RigidbodyType2D.Dynamic;
        }
    }

    private void OnTouchEnded(InputAction.CallbackContext context)
    {
        isDragging = false;

        // Change Rigidbody2D back to Kinematic after dragging is done
        rb2d.bodyType = RigidbodyType2D.Kinematic;

        // Reset the position to the original one and log it
        transform.position = originalPosition; 
        transform.rotation = Quaternion.identity; // Reset rotation
    }

    void Update()
    {   
        if (isDragging)
        {
            Vector2 inputPosition = Application.isMobilePlatform
                ? Touchscreen.current.primaryTouch.position.ReadValue()
                : Mouse.current.position.ReadValue();

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(inputPosition);
            worldPos.z = 0;

            // Manually update the position of the soap object while dragging
            transform.position = worldPos + offset; // Keep the offset to follow the touch or mouse position
        }
    }
}
