using UnityEngine;
using System.Collections;

public class SoundManager : MonoBehaviour
{
    // Reference to the AudioSource component
    private AudioSource audioSource;
    public AudioSource BGXSource;
    public AudioClip[] backgroundMusic; // [0] = default, [1] = alternate
    public AudioClip[] dogPant;
    public AudioClip boomerang; // still not used
    public AudioClip ballBounce; // same
    public AudioClip lightSwitch;
    public AudioClip Faucet;
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
    private int currentBGMIndex = 0; // Track which BGM is playing (0 = default, 1 = alternate)

    void Start()
    {
        // Get the AudioSource component attached to this GameObject
        audioSource = GetComponent<AudioSource>();

        // Get the pet type from PlayerPrefs
        pet_type = PlayerPrefs.GetString(petKey + "PetType");

        // Play the default background music (first one in array)
        PlayBGM(0);

        // Start the random sound play coroutine
        StartCoroutine(PlayRandomPetSounds());
    }

    // Play specific BGM by index and loop it
    private void PlayBGM(int index)
    {
        if (index < 0 || index >= backgroundMusic.Length)
        {
            Debug.LogError($"BGM index {index} is out of range! Array length: {backgroundMusic.Length}");
            return;
        }

        currentBGMIndex = index;
        BGXSource.clip = backgroundMusic[index];
        BGXSource.loop = true; // Loop the BGM
        BGXSource.Play();
    }

    // Public method to switch to alternate BGM (index 1)
    public void SwitchToAlternateBGM()
    {
        if (currentBGMIndex != 1)
        {
            PlayBGM(1);
        }
    }

    // Public method to switch back to default BGM (index 0)
    public void SwitchToDefaultBGM()
    {
        if (currentBGMIndex != 0)
        {
            PlayBGM(0);
        }
    }

    // Public method to toggle between default and alternate BGM
    public void ToggleBGM()
    {
        if (currentBGMIndex == 0)
        {
            SwitchToAlternateBGM();
        }
        else
        {
            SwitchToDefaultBGM();
        }
    }

    // Public method to switch to a specific BGM by index
    public void SwitchBGM(int index)
    {
        PlayBGM(index);
    }

    // Get current BGM index
    public int GetCurrentBGMIndex()
    {
        return currentBGMIndex;
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
                int randomIndex = Random.Range(0, dogPant.Length);
                audioSource.PlayOneShot(dogPant[randomIndex]);
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
    }

    public void PlayFaucet()
    {
        audioSource.PlayOneShot(Faucet);
    }

    public void PlayLightSound()
    {
        audioSource.PlayOneShot(lightSwitch);
    }
}

/* 
USAGE EXAMPLES:

// In MiniGame.cs or any other script:
public SoundManager soundManager;

void StartMiniGame()
{
    soundManager.SwitchToAlternateBGM(); // Switch to BGM index 1
}

void EndMiniGame()
{
    soundManager.SwitchToDefaultBGM(); // Switch back to BGM index 0
}

// Or use toggle:
soundManager.ToggleBGM(); // Switches between 0 and 1

// Or switch to specific index:
soundManager.SwitchBGM(0); // Play first BGM
soundManager.SwitchBGM(1); // Play second BGM
soundManager.SwitchBGM(2); // Play third BGM (if you add more)
*/