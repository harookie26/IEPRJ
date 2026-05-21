using UnityEngine;
using Game.ObjectTypes;

[FoldableInspector]
public class BaseCollectible : MonoBehaviour, ICollectible
{
    [Header("Collectible")]
    [SerializeField, Tooltip("Unique ID for this collectible (set in the Inspector).")]
    private string collectibleId = "Collectible";

    [Header("Levitation Settings")]
    [SerializeField, Tooltip("Should this collectible start levitating automatically.")]
    private bool startLevitating = true;

    [SerializeField, Tooltip("Maximum displacement from the start position.")]
    [Range(0f, 2f)]
    private float amplitude = 0.25f;

    [SerializeField, Tooltip("How fast the collectible bobs (cycles per second).")]
    [Range(0.1f, 5f)]
    private float frequency = 1f;

    [SerializeField, Tooltip("Axis along which the collectible bobs.")]
    private Vector3 bobAxis = Vector3.up;

    [SerializeField, Tooltip("Rotation speed in degrees per second.")]
    private float rotationSpeed = 45f;

    [SerializeField, Tooltip("Optional random time offset so multiple collectibles don't bob in unison.")]
    private float randomizePhase = 0.15f;

    // Internal state
    private Vector3 startPosition;
    private float timeAccumulator;
    private bool isLevitating;

    private void Awake()
    {
        startPosition = transform.position;
        timeAccumulator = Random.Range(0f, randomizePhase);
        isLevitating = startLevitating;
    }

    void Start()
    {
        // 1. Subscribe to the load event. If the save file finishes downloading AFTER this object spawns, it will hear the shout.
        if (PlayerCollectibleManager.Instance != null)
        {
            PlayerCollectibleManager.Instance.OnCollectiblesLoaded += CheckIfAlreadyCollected;
        }

        // 2. Also check immediately, just in case the save file loaded BEFORE this object spawned.
        CheckIfAlreadyCollected();
    }

    private void OnDestroy()
    {
        // ALWAYS unsubscribe from events to prevent memory leaks!
        if (PlayerCollectibleManager.Instance != null)
        {
            PlayerCollectibleManager.Instance.OnCollectiblesLoaded -= CheckIfAlreadyCollected;
        }
    }

    private void CheckIfAlreadyCollected()
    {
        if (PlayerCollectibleManager.Instance != null && PlayerCollectibleManager.Instance.HasCollected(collectibleId))
        {
            // If the manager remembers we picked this up, destroy the physical 3D object so we can't pick it up again!
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isLevitating)
            ApplyLevitation();
    }

    private void ApplyLevitation()
    {
        timeAccumulator += Time.deltaTime;
        float bob = Mathf.Sin(timeAccumulator * frequency * Mathf.PI * 2f) * amplitude;
        Vector3 offset = (bobAxis.normalized) * bob;
        transform.position = startPosition + offset;

        if (rotationSpeed != 0f)
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }

    // ICollectible implementation
    public virtual void Collect()
    {
        var manager = FindFirstObjectByType<PlayerCollectibleManager>();
        if (manager != null)
        {
            manager.AddCollected(GetID);
        }
        else
        {
            Debug.LogWarning($"PlayerCollectibleManager not found when collecting {GetID}.");
        }

        OnCollect();

        if(GetID == "Key")
        {
            DialogueTriggerManager.Instance.TriggerKeyFoundDialogue();
        }

        if (GetID == "Paintbucket")
        {
            EventBroadcaster.Instance.PostEvent(EventNames.HintEvents.HINT_PAINTING_START);
            DialogueTriggerManager.Instance.TriggerChannelDialogue();
        }


        Destroy(gameObject);
    }

    // Hook for extra behavior on collect
    protected virtual void OnCollect() { }

    public virtual void Levitate()
    {
        isLevitating = true;
    }

    // Exposed ID (can be overridden by derived classes)
    public virtual string GetID => collectibleId;
}