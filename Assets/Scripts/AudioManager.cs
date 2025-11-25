using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioMixer audioMixer;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Mute Button Settings")]
    public Image muteButtonImage;   
    public Sprite soundOnIcon;      
    public Sprite soundOffIcon;     

    private const float minVolume = 0.0001f;
    private const float muteVolume = -80f;
    private bool isMuted = false;

    void Start()
    {
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);

        musicSlider.value = musicVol;
        sfxSlider.value = sfxVol;

        isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;

        ApplyMusicVolume(musicVol);
        ApplySFXVolume(sfxVol);
        UpdateMuteState(); 

        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        UpdateMuteState();

        PlayerPrefs.SetInt("IsMuted", isMuted ? 1 : 0);
    }

    private void UpdateMuteState()
    {
        if (isMuted)
        {
            AudioListener.volume = 0; 
            if (muteButtonImage != null) muteButtonImage.sprite = soundOffIcon;
        }
        else
        {
            AudioListener.volume = 1; 
            if (muteButtonImage != null) muteButtonImage.sprite = soundOnIcon;
        }
    }

    public void SetMusicVolume(float volume)
    {
        ApplyMusicVolume(volume);
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    public void SetSFXVolume(float volume)
    {
        ApplySFXVolume(volume);
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }

    private void ApplyMusicVolume(float volume)
    {
        float dB = (volume <= minVolume) ? muteVolume : Mathf.Log10(volume) * 20;
        audioMixer.SetFloat("MusicVolume", dB);
    }

    private void ApplySFXVolume(float volume)
    {
        float dB = (volume <= minVolume) ? muteVolume : Mathf.Log10(volume) * 20;
        audioMixer.SetFloat("SFXVolume", dB);
    }
}