using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer myMixer;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    public void SetMasterVolume()
    {
        float masterVolume =masterSlider.value;
        myMixer.SetFloat("MasterVolParam", Mathf.Log10(masterVolume) * 20);
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
    }

    public void SetSFXVolume()
    {
        float sfxVolume = sfxSlider.value;
        myMixer.SetFloat("SFXVolParam", Mathf.Log10(sfxVolume) * 20);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }

    public void SetMusicVolume()
    {
        float musicVolume = musicSlider.value;
        myMixer.SetFloat("MusicVolParam", Mathf.Log10(musicVolume) * 20);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
    }

    private void LoadVolume()
    {
        masterSlider.value = PlayerPrefs.GetFloat("MasterVolume");
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume");
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume");

        SetMasterVolume();
        SetSFXVolume();
        SetMusicVolume();
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(PlayerPrefs.HasKey("MasterVolume") && PlayerPrefs.HasKey("SFXVolume") && PlayerPrefs.HasKey("MusicVolume"))
        {
            LoadVolume();
        }
        else
        {
            SetMasterVolume();
            SetSFXVolume();
            SetMusicVolume();
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
