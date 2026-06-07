using Game.States;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionTrigger : MonoBehaviour, ISaveable
{
    private AudioSource sfxAudioSource;

    private static readonly Dictionary<string, InteractionTrigger> Registry = new();

    [Header("Identification")]
    [SerializeField] private string uniqueID;
    public static InteractionTrigger Instance { get; private set; }

    [SerializeField] private bool isflashlightHintTrigger = false;

    [SerializeField] private GameObject model;
    [SerializeField] private AudioClip sfx;
    [SerializeField] private EnemyStateMachine enemyStateMachine;

    [Header("Animation Setup")]
    [SerializeField] bool hasAnimation = false;
    [SerializeField] bool disableAfterAnimation = false;
    [SerializeField] private Animator modelAnimator;
    [SerializeField] private string animationStateName = "TriggerAnimation";

    public bool hasTriggered = false;
    public string SaveKey => uniqueID;
    private void Awake()
    {
        GameObject audioObject1 = GameObject.FindWithTag("SFXAudioSource");

        if (audioObject1 != null)
        {
            sfxAudioSource = audioObject1.GetComponent<AudioSource>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'SFXAudioSource' found in scene.");
        }

        if (hasAnimation && modelAnimator == null && model != null)
        {
            modelAnimator = model.GetComponent<Animator>();
        }

        Register();
    }

    private void OnDestroy()
    {
        Unregister();
    }

    private void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            if (isflashlightHintTrigger)
            {
                DialogueTriggerManager.Instance.TriggerEnemyIntroDialogue();
            }
            sfxAudioSource.PlayOneShot(sfx);

            EnemyManager manager = FindFirstObjectByType<EnemyManager>();
            if (manager != null)
            {
                manager.ReportTriggerActivated();
            }
            else
            {
                Debug.LogError("[InteractionTrigger] CRITICAL: Could not find EnemyManager in the scene!");
            }

            if (hasAnimation && modelAnimator != null)
            {
                StartCoroutine(PlayAnimationThenDeactivate());
            }
            else
            {
                if (model != null) model.SetActive(false);
            }
        }
    }

    private IEnumerator PlayAnimationThenDeactivate()
    {
        modelAnimator.Play(animationStateName);

        yield return null;

        float animationLength = modelAnimator.GetCurrentAnimatorStateInfo(0).length;

        yield return new WaitForSeconds(animationLength);

        if (model != null)
        {
            //if(disableAfterAnimation)
            //    model.SetActive(false);
            //else
            //    model.SetActive(true);
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

        if (this.hasTriggered && model != null)
        {
            model.SetActive(false);
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

        if (this.hasTriggered && model != null)
        {
            model.SetActive(false);
        }
    }
}