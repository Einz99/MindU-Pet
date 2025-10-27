using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Soap : MonoBehaviour
{
    private InputAction clickAction;
    private InputAction positionAction; // Add this for position tracking
    private bool isDragging = false;
    private Vector3 offset;
    private Rigidbody2D rb2d;
    public Sprite[] Soaps = new Sprite[4];
    public TMP_Text Soapquantity;

    private Vector3 originalPosition = new Vector3(-1.65f, 2.9f, 0);

    private void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        transform.position = originalPosition;
        rb2d.bodyType = RigidbodyType2D.Kinematic;

        EnablingActions();
    }

    private void OnDisable()
    {
        clickAction.Disable();
        positionAction.Disable();
    }

    private void OnEnable()
    {
        EnablingActions();
        string petkey = PlayerPrefKeys.PetPrefix;
        int soap_type = PlayerPrefs.GetInt(petkey + PlayerPrefKeys.soap_type);
        int soap_quantity = PlayerPrefs.GetInt(petkey + PlayerPrefKeys.soap_quantity);

        GetComponent<SpriteRenderer>().sprite = Soaps[soap_type];
        Soapquantity.text = soap_quantity + "x";
    }
    
    private void EnablingActions()
    {
        if (Application.isMobilePlatform)
        {
            clickAction = new InputAction(type: InputActionType.Button, binding: "<Touchscreen>/primaryTouch/press");
            positionAction = new InputAction(type: InputActionType.Value, binding: "<Touchscreen>/primaryTouch/position");
            clickAction.performed += OnTouchBegan;
            clickAction.canceled += OnTouchEnded;
            clickAction.Enable();
            positionAction.Enable();
        }
        else
        {
            clickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
            positionAction = new InputAction(type: InputActionType.Value, binding: "<Mouse>/position");
            clickAction.performed += OnTouchBegan;
            clickAction.canceled += OnTouchEnded;
            clickAction.Enable();
            positionAction.Enable();
        }
    }

    private void OnTouchBegan(InputAction.CallbackContext context)
    {
        Vector2 inputPos = positionAction.ReadValue<Vector2>();
        Vector3 worldTouchPos = Camera.main.ScreenToWorldPoint(inputPos);
        worldTouchPos.z = 0;

        RaycastHit2D hit = Physics2D.Raycast(worldTouchPos, Vector2.zero);

        if (hit.collider != null && hit.collider.gameObject == gameObject)
        {
            isDragging = true;
            offset = gameObject.transform.position - worldTouchPos;
            rb2d.bodyType = RigidbodyType2D.Dynamic;
        }
    }

    private void OnTouchEnded(InputAction.CallbackContext context)
    {
        isDragging = false;
        rb2d.bodyType = RigidbodyType2D.Kinematic;
        transform.position = originalPosition; 
        transform.rotation = Quaternion.identity;
    }

    void Update()
    {   
        if (isDragging)
        {
            Vector2 inputPosition = positionAction.ReadValue<Vector2>();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(inputPosition);
            worldPos.z = 0;
            transform.position = worldPos + offset;
        }
    }
}