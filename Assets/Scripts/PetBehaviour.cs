using TMPro;
using UnityEngine;

public class PetBehaviour : MonoBehaviour
{
    [Header("Pet Movement and Scaling Factors")]
    private float moveSpeed = 1f;
    private float scaleFactor = 2f;
    private float changeDirectionTimeMin = 15f;
    private float changeDirectionTimeMax = 30f;

    [Header("Location and Conditions")]
    private Vector3 randomizedPosition;
    private Vector3 initialScale;
    private bool isMoving = false;
    private bool isIdle = true;
    private bool isPositionSet = false; 

    [Header("Animator and Controllers")]
    private Animator animator;
    public TMP_Text petNameTxt;
    public RuntimeAnimatorController[] controllers;
    private Pet pet;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();

        pet = new Pet();
        reflectPetData();
    }

    private void reflectPetData()
    {
        // Use a single key for the pet data (no need for arrays)
        string petKey = PlayerPrefKeys.PetPrefix; // Unique pet key, assuming only one pet

        // Retrieve data from PlayerPrefs and assign it to the single pet object
        pet.id = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);
        pet.student_id = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetStudentID);
        pet.pet_name = PlayerPrefs.GetString(petKey + PlayerPrefKeys.PetName);
        pet.pet_type = PlayerPrefs.GetString(petKey + PlayerPrefKeys.PetType);
        pet.coins = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetCoins);
        pet.food_stack = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetFoodStack);
        pet.hygiene_stack = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHygieneStack);
        pet.pet_head = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHead);
        pet.pet_neck = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetNeck);
        pet.pet_eyes = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetEyes);
        pet.hunger = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHunger);
        pet.playfulness = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetPlayfulness);
        pet.hygiene = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetHygiene);
        pet.sleep = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetSleep);
        pet.created_at = PlayerPrefs.GetString(petKey + PlayerPrefKeys.PetCreatedAt);
        pet.updated_at = PlayerPrefs.GetString(petKey + PlayerPrefKeys.PetUpdatedAt);
        switch (pet.pet_type)
        {
            case "cat_1": animator.runtimeAnimatorController = controllers[0]; break;
            case "cat_2": animator.runtimeAnimatorController = controllers[1]; break;
            case "cat_3": animator.runtimeAnimatorController = controllers[2]; break;
            case "dog_1": animator.runtimeAnimatorController = controllers[3]; break;
            case "dog_2": animator.runtimeAnimatorController = controllers[4]; break;
            case "dog_3": animator.runtimeAnimatorController = controllers[5]; break;
            default: return;
        }
        petNameTxt.text = pet.pet_name;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}