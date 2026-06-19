using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer myMixer;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private bool isAdjustingSFXSlider;

    public void SetMasterVolume()
    {
        float masterVolume = masterSlider.value;
        myMixer.SetFloat("MasterVolParam", ToDecibels(masterVolume));
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
    }

    public void SetSFXVolume()
    {
        float sfxVolume = sfxSlider.value;
        myMixer.SetFloat("SFXVolParam", ToDecibels(sfxVolume));
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }

    public void SetMusicVolume()
    {
        float musicVolume = musicSlider.value;
        myMixer.SetFloat("MusicVolParam", ToDecibels(musicVolume));
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
        RegisterSFXSliderReleaseSFX();

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

    private void RegisterSFXSliderReleaseSFX()
    {
        if (sfxSlider == null)
        {
            return;
        }

        EventTrigger trigger = sfxSlider.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = sfxSlider.gameObject.AddComponent<EventTrigger>();
        }

        AddEventTriggerEntry(trigger, EventTriggerType.PointerDown, _ => isAdjustingSFXSlider = true);
        AddEventTriggerEntry(trigger, EventTriggerType.PointerUp, _ => PlaySFXSliderReleaseSFX());
        AddEventTriggerEntry(trigger, EventTriggerType.EndDrag, _ => PlaySFXSliderReleaseSFX());
    }

    private void PlaySFXSliderReleaseSFX()
    {
        if (!isAdjustingSFXSlider)
        {
            return;
        }

        isAdjustingSFXSlider = false;
        SetSFXVolume();

        if (AudioList.Current != null)
        {
            AudioList.Current.PlaySFXSettingAdjustment();
        }
    }

    private void AddEventTriggerEntry(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = type
        };
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    private float ToDecibels(float volume)
    {
        return Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20;
    }
}
