using UnityEngine;

public class EnemyIntroTrigger : MonoBehaviour
{
    private AudioSource sfxAudioSource;

    public static EnemyIntroTrigger Instance { get; private set; }

    [SerializeField] private GameObject enemyIntroModel;
    [SerializeField] private AudioClip lightsOutSFX;

    public bool hasTriggered = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        GameObject audioObject1 = GameObject.FindWithTag("SFXAudioSource");

        if (audioObject1 != null)
        {

            sfxAudioSource = audioObject1.GetComponent<AudioSource>();
            sfxAudioSource.clip = lightsOutSFX;
        }
        else
        {
            //.
            Debug.LogWarning("No GameObject with tag 'SFXAudioSource' found in scene.");
        }
    }

    void Start()
    {
        if (hasTriggered)
        {
            enemyIntroModel.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            EventBroadcaster.Instance.PostEvent(EventNames.HintEvents.HINT3_START);
            DialogueTriggerManager.Instance.TriggerEnemyIntroDialogue();
            enemyIntroModel.SetActive(false);
            sfxAudioSource.Play();
        }
    }

    public EnemyIntroSaveData GetSaveData()
    {
        return new EnemyIntroSaveData
        {
            hasTriggered = this.hasTriggered
        };
    }

    public void LoadSaveData(EnemyIntroSaveData data)
    {
        if (data == null) return;

        this.hasTriggered = data.hasTriggered;

        // Apply the visual state IMMEDIATELY upon loading the save data
        if (this.hasTriggered && enemyIntroModel != null)
        {
            enemyIntroModel.SetActive(false);
        }
    }

}