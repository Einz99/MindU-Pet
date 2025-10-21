using UnityEngine;
using System.Collections;

public class SoundManager : MonoBehaviour
{
    // Reference to the AudioSource component
    private AudioSource audioSource;
    public AudioSource BGXSource;
    public AudioClip backgroundMusic;
    public AudioClip dogPant;
    public AudioClip boomerang; // still not used
    public AudioClip ballBounce; // same
    public AudioClip[] catMeows;
    public AudioClip digitalClick; // same
    public AudioClip eating; 
    public AudioClip toySqueek; // same
    public AudioClip coin;
    public AudioClip windupClock; // same
    public AudioClip shower;
    public AudioClip bubble;

    private string petKey = "PetPrefix"; // Unique pet key, assuming only one pet
    private string pet_type;

    void Start()
    {
        // Get the AudioSource component attached to this GameObject
        audioSource = GetComponent<AudioSource>();

        // Set the AudioSource properties for background music (BGX)
        BGXSource.clip = backgroundMusic;
        BGXSource.loop = true; // Loop the background music
        BGXSource.volume = 0.5f; // Set volume for background music

        // Play the background music
        BGXSource.Play();

        // Get the pet type from PlayerPrefs
        pet_type = PlayerPrefs.GetString(petKey + "PetType");

        // Start the random sound play coroutine
        StartCoroutine(PlayRandomPetSounds());
    }

    // Coroutine to randomly play pet sounds every 30-60 seconds
    private IEnumerator PlayRandomPetSounds()
    {
        while (true)
        {
            float randomTime = Random.Range(30f, 60f); // Randomize between 30 and 60 seconds
            yield return new WaitForSeconds(randomTime);

            // Check if pet is a cat (cat_1, cat_2, cat_3)
            if (pet_type == "cat_1" || pet_type == "cat_2" || pet_type == "cat_3")
            {
                // Randomize a cat meow sound
                int randomIndex = Random.Range(0, catMeows.Length);
                audioSource.PlayOneShot(catMeows[randomIndex]);
            }
            // Check if pet is a dog (dog_1, dog_2, dog_3)
            else if (pet_type == "dog_1" || pet_type == "dog_2" || pet_type == "dog_3")
            {
                // Play the dog pant sound
                audioSource.PlayOneShot(dogPant);
            }
        }
    }

    // Method to play the coin sound
    public void PlayCoinSound()
    {
        audioSource.PlayOneShot(coin);
    }

    // Method to play the shower sound for 10 seconds
    public void PlayShowerSoundForDuration(float duration = 10f)
    {
        StartCoroutine(PlaySoundForDuration(shower, duration));
    }

    public void PlayBubble()
    {
        audioSource.PlayOneShot(bubble);
    }

    // Method to play the eating sound for 8 seconds
    public void PlayEatingSoundForDuration(float duration = 8f)
    {
        StartCoroutine(PlaySoundForDuration(eating, duration));
    }

    // Coroutine to play a sound for a set duration
    private IEnumerator PlaySoundForDuration(AudioClip sound, float duration)
    {
        audioSource.PlayOneShot(sound);  // Play the sound
        yield return new WaitForSeconds(duration);  // Wait for the duration
        // No need to stop explicitly since PlayOneShot won't overlap, but you can stop it here if needed
    }
}
