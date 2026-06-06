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

    [SerializeField] private GameObject model;
    [SerializeField] private AudioClip sfx;
    [SerializeField] private EnemyStateMachine enemyStateMachine;

    [Header("Animation Setup")]
    [SerializeField] bool hasAnimation = false;
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
        //if (hasTriggered)
        //{
        //    model.SetActive(false);
        //}
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            DialogueTriggerManager.Instance.TriggerEnemyIntroDialogue();
            sfxAudioSource.PlayOneShot(sfx);

            enemyStateMachine.scriptedEncounterCheck();

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
        // 1. Play the animation sequence (supports using a State Name or a Trigger)
        modelAnimator.Play(animationStateName);

        // Wait exactly 1 frame to ensure the animator updates and transitions into the new state
        yield return null;

        // 2. Dynamically calculate the duration of the current animation clip playing
        float animationLength = modelAnimator.GetCurrentAnimatorStateInfo(0).length;

        // 3. Wait safely until the sequence finishes playing
        yield return new WaitForSeconds(animationLength);

        // 4. Finally clean up and deactivate the object
        if (model != null)
        {
            model.SetActive(false);
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