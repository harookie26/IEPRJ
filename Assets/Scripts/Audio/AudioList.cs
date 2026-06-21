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

    [Header("Player Caught SFX")]
    [SerializeField] private AudioClip playerCaughtSFX;

    [Header("Ambient Music")]
    [Tooltip("Loops from scene start until the Enemy Intro Trigger is encountered.")]
    [SerializeField] private AudioClip preEnemyIntroAmbient;
    [Tooltip("Loops after the Enemy Intro Trigger is encountered.")]
    [SerializeField] private AudioClip postEnemyIntroAmbient;

    [Header("Testing Clips")]
    [SerializeField] public AudioClip testMusic;
    [SerializeField] public AudioClip testSFX;

    [Header("Audio Sources")]
    [Tooltip("Dedicated AudioSource for restoration music.")]
    [SerializeField] public AudioSource restorationMusicAudioSource;

    private AudioSource sfxAudioSource;
    private AudioSource musicAudioSource;
    private AudioSource surpriseEncounterAudioSource;
    private bool playerCaughtSFXActive;

    private void Awake()
    {
        Current = this;
        ResolveSFXAudioSource();
        ResolveMusicAudioSource();
        PlayAmbient(preEnemyIntroAmbient);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EventBroadcaster.Instance.AddObserver(EventNames.EnemyEvents.ENEMY_CATCHED, PlaySurpriseEncounterSFX);
    }

    private void Start()
    {
        RegisterButtonClickSFX();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        EventBroadcaster.Instance.RemoveActionAtObserver(EventNames.EnemyEvents.ENEMY_CATCHED, PlaySurpriseEncounterSFX);
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

    public void PlayPreEnemyIntroAmbient()
    {
        PlayAmbient(preEnemyIntroAmbient);
    }

    public void PlayPostEnemyIntroAmbient()
    {
        PlayAmbient(postEnemyIntroAmbient);
    }

    private void PlaySurpriseEncounterSFX()
    {
        if (playerCaughtSFX == null || playerCaughtSFXActive)
        {
            return;
        }

        playerCaughtSFXActive = true;
        ResolveSurpriseEncounterAudioSource();
        surpriseEncounterAudioSource.clip = playerCaughtSFX;
        surpriseEncounterAudioSource.Play();
    }

    public void StopSurpriseEncounterSFX()
    {
        playerCaughtSFXActive = false;

        if (surpriseEncounterAudioSource == null)
        {
            return;
        }

        surpriseEncounterAudioSource.Stop();
        surpriseEncounterAudioSource.clip = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveSFXAudioSource();
        ResolveMusicAudioSource();
        RegisterButtonClickSFX();
    }

    private void PlayAmbient(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        ResolveMusicAudioSource();
        if (musicAudioSource == null ||
            (musicAudioSource.clip == clip && musicAudioSource.isPlaying))
        {
            return;
        }

        musicAudioSource.Stop();
        musicAudioSource.clip = clip;
        musicAudioSource.loop = true;
        musicAudioSource.Play();
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

    private void ResolveMusicAudioSource()
    {
        if (musicAudioSource != null)
        {
            return;
        }

        GameObject audioObject = GameObject.FindWithTag("MusicAudioSource");
        if (audioObject != null)
        {
            musicAudioSource = audioObject.GetComponent<AudioSource>();
        }
    }

    private void ResolveSurpriseEncounterAudioSource()
    {
        if (surpriseEncounterAudioSource != null)
        {
            return;
        }

        ResolveSFXAudioSource();
        surpriseEncounterAudioSource = gameObject.AddComponent<AudioSource>();
        surpriseEncounterAudioSource.playOnAwake = false;
        surpriseEncounterAudioSource.loop = false;
        surpriseEncounterAudioSource.spatialBlend = 0f;

        if (sfxAudioSource != null)
        {
            surpriseEncounterAudioSource.outputAudioMixerGroup = sfxAudioSource.outputAudioMixerGroup;
        }
    }
}
