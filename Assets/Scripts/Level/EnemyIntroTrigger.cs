using System.Collections;
using UnityEngine;

public class EnemyIntroTrigger : MonoBehaviour
{
    public static EnemyIntroTrigger Instance { get; private set; }

    [SerializeField] private GameObject enemyIntroModel;

    public bool hasTriggered = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        if(hasTriggered)
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