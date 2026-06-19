using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioList : MonoBehaviour
{
    public static AudioList Current { get; private set; }

    //Add here music clips and SFX clips to be used in the game

    [Header("Locked Door Clip")]
    [SerializeField] public AudioClip lockedDoorSFX;

    [Header("Unlock Door Clip")]
    [SerializeField] public AudioClip unlockDoorSFX;

    [Header("Painting Restoration Music Clip")]
    [SerializeField] public AudioClip paintingRestorationMusic;

    [Header("Painting Restoration Complete SFX Clip")]
    [SerializeField] public AudioClip paintingRestorationCompleteSFX;

    [Header("Enemy Distracted SFX Clip")]
    [SerializeField] public AudioClip enemyDistractedSFX;

    [Header("Player Collectible SFX Clip")]
    [SerializeField] public AudioClip playerCollectibleSFX;

    [Header("Elevator Use SFX Clip")]
    [SerializeField] public AudioClip elevatorSFX;

    [Header("Map + Newspaper Pickup")]
    [SerializeField] public AudioClip paperPickupSFX;

    [Header("Objectives SFX")]
    [SerializeField] public AudioClip objectivesSFX;

    [Header("Key Pickup SFX")]
    [SerializeField] public AudioClip keyPickupSFX;

    [Header("Scream SFX")]
    [SerializeField] public AudioClip screamSFX;

    [Header("Button Click SFX")]
    [SerializeField] public AudioClip buttonClickSFX;

    [Header("SFX Setting Adjustment")]
    [SerializeField] public AudioClip sfxSettingAdjustment;

    [Header("Testing Clips")]
    [SerializeField] public AudioClip testMusic;
    [SerializeField] public AudioClip testSFX;

    [Header("Audio Sources")]
    [Tooltip("Dedicated AudioSource for restoration music.")]
    [SerializeField] public AudioSource restorationMusicAudioSource;

    private AudioSource sfxAudioSource;

    private void Awake()
    {
        Current = this;
        ResolveSFXAudioSource();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        RegisterButtonClickSFX();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Current == this)
        {
            Current = null;
        }
    }

    public void PlayButtonClickSFX()
    {
        PlaySFX(buttonClickSFX);
    }

    public void PlaySFXSettingAdjustment()
    {
        PlaySFX(sfxSettingAdjustment);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        ResolveSFXAudioSource();
        if (sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(clip);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveSFXAudioSource();
        RegisterButtonClickSFX();
    }

    private void RegisterButtonClickSFX()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            button.onClick.RemoveListener(PlayButtonClickSFX);
            button.onClick.AddListener(PlayButtonClickSFX);
        }
    }

    private void ResolveSFXAudioSource()
    {
        if (sfxAudioSource != null)
        {
            return;
        }

        GameObject audioObject = GameObject.FindWithTag("SFXAudioSource");
        if (audioObject != null)
        {
            sfxAudioSource = audioObject.GetComponent<AudioSource>();
        }
    }
}
