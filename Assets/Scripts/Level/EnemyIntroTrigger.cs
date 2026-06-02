using Game.States;
using System.Collections.Generic;
using UnityEngine;

public class EnemyIntroTrigger : MonoBehaviour, ISaveable
{
    private AudioSource sfxAudioSource;

    private static readonly Dictionary<string, EnemyIntroTrigger> Registry = new();

    [Header("Identification")]
    [SerializeField] private string uniqueID;
    public static EnemyIntroTrigger Instance { get; private set; }

    [SerializeField] private GameObject enemyIntroModel;
    [SerializeField] private AudioClip lightsOutSFX;
    [SerializeField] private EnemyStateMachine enemyStateMachine;

    public bool hasTriggered = false;
    public string SaveKey => uniqueID;
    private void Awake()
    {
        //if (Instance != null && Instance != this)
        //{
        //    Destroy(gameObject);
        //    return;
        //}

        //Instance = this;

        GameObject audioObject1 = GameObject.FindWithTag("SFXAudioSource");

        if (audioObject1 != null)
        {

            sfxAudioSource = audioObject1.GetComponent<AudioSource>();
            //sfxAudioSource.clip = lightsOutSFX;
        }
        else
        {
            //.
            Debug.LogWarning("No GameObject with tag 'SFXAudioSource' found in scene.");
        }

        Register();
    }

    private void OnDestroy()
    {
        Unregister();
    }

    private void Update()
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
            DialogueTriggerManager.Instance.TriggerEnemyIntroDialogue();
            enemyIntroModel.SetActive(false);
            sfxAudioSource.PlayOneShot(lightsOutSFX);

            enemyStateMachine.scriptedEncounterCheck();
        }
    }

    private void Register()
    {
        if (string.IsNullOrWhiteSpace(uniqueID))
        {
            Debug.LogError($"EnemyIntroTrigger on {gameObject.name} has no unique ID.");
            return;
        }

        if (Registry.ContainsKey(uniqueID))
        {
            Debug.LogError($"Duplicate EnemyIntroTrigger ID detected: {uniqueID}");
            return;
        }

        Registry.Add(uniqueID, this);
        GlobalSaveSystem.Register(this);
    }

    private void Unregister()
    {
        if (Registry.ContainsKey(uniqueID))
            Registry.Remove(uniqueID);

        GlobalSaveSystem.Unregister(this);
    }

    public object CaptureState()
    {
        return new EnemyIntroSaveData
        {
            hasTriggered = this.hasTriggered
        };
    }

    public void RestoreState(object state)
    {
        if (state == null) return;

        var data = (EnemyIntroSaveData)state;
        this.hasTriggered = data.hasTriggered;

        // Apply visual state immediately upon data restoration
        if (this.hasTriggered && enemyIntroModel != null)
        {
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