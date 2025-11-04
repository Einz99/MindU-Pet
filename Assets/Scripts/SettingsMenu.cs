using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public AudioSource musicSource; // Drag BGM AudioSource here
    public AudioSource soundSource; // Drag SFX AudioSource here
    
    public Toggle musicToggle; // Drag Music Toggle here
    public Toggle soundToggle; // Drag Sound Toggle here
    public Slider volumeSlider; // Drag Volume Slider here
    
    private void Start()
    {
        // Load settings on startup
        bool musicMuted = PlayerPrefs.GetInt("MuteMusic", 0) == 1;
        bool soundMuted = PlayerPrefs.GetInt("MuteSound", 0) == 1;
        float volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        
        // Update UI to match saved settings
        if (musicToggle != null)
            musicToggle.isOn = !musicMuted; // Toggle is ON when NOT muted
        if (soundToggle != null)
            soundToggle.isOn = !soundMuted;
        if (volumeSlider != null)
            volumeSlider.value = volume;
        
        // Apply settings to audio sources
        ApplyMusicMute(musicMuted);
        ApplySoundMute(soundMuted);
        ApplyMasterVolume(volume);
        
        // Add listeners for UI changes
        if (musicToggle != null)
            musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        if (soundToggle != null)
            soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
    }
    
    private void OnMusicToggleChanged(bool isOn)
    {
        bool muted = !isOn;
        StorageBridge.Instance.SaveValue("MuteMusic", muted ? 1 : 0); // ← USE THIS INSTEAD
        ApplyMusicMute(muted);
    }
    
    private void OnSoundToggleChanged(bool isOn)
    {
        bool muted = !isOn;
        StorageBridge.Instance.SaveValue("MuteSound", muted ? 1 : 0); // ← USE THIS INSTEAD
        ApplySoundMute(muted);
    }
    
    private void OnVolumeSliderChanged(float value)
    {
        StorageBridge.Instance.SaveValue("MasterVolume", value); // ← USE THIS INSTEAD
        ApplyMasterVolume(value);
    }
    
    private void ApplyMusicMute(bool muted)
    {
        if (musicSource != null)
            musicSource.mute = muted;
    }
    
    private void ApplySoundMute(bool muted)
    {
        if (soundSource != null)
            soundSource.mute = muted;
    }
    
    private void ApplyMasterVolume(float volume)
    {
        if (musicSource != null)
            musicSource.volume = volume;
        if (soundSource != null)
            soundSource.volume = volume;
    }
}