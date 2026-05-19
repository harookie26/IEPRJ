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

    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            DialogueTriggerManager.Instance.TriggerEnemyIntroDialogue();
            enemyIntroModel.SetActive(false);
        }
    }

}