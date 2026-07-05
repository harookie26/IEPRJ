using System.Collections;
using Game.ObjectTypes;
using UnityEngine;

public class DrawerInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject drawerObject;

    [Header("Sliding Settings")]
    [Tooltip("How far and in what direction the drawer should slide. Adjust X, Y, or Z as needed.")]
    [SerializeField] private Vector3 openOffset = new Vector3(-0.3f, 0f, 0f);
    [Tooltip("How fast the drawer slides open.")]
    [SerializeField] private float slideSpeed = 2f;

    [SerializeField] private GameObject keyCollectible;

    [Header("Interaction Gate")]
    [SerializeField] private Transform interactionAnchor;
    [SerializeField, Min(0.1f)] private float maxInteractDistance = 2f;
    [SerializeField, Range(-1f, 1f)] private float frontDotThreshold = 0.2f;

    private AudioSource sfxAudioSource;
    private AudioList audioList;

    public bool hasOpened = false;
    private bool isSliding = false;

    private Vector3 closedPosition;
    private Vector3 openPosition;

    private void Awake()
    {
        audioList = FindAnyObjectByType<AudioList>();
        GameObject audioObject = GameObject.FindWithTag("SFXAudioSource");

        if (audioObject != null)
        {
            sfxAudioSource = audioObject.GetComponent<AudioSource>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'SFXAudioSource' found in scene.");
        }

        // --- FIX: Moved from Start() to Awake() ---
        // Record positions immediately so they are ready BEFORE the save file loads
        if (drawerObject != null)
        {
            closedPosition = drawerObject.transform.position;
            openPosition = closedPosition + openOffset;
        }

        SetKeyColliderEnabled(hasOpened);
    }

    void Start()
    {
        // We keep this here just in case the drawer was manually set to 'hasOpened = true' 
        // in the Inspector, or if the save data loaded very late.
        if (hasOpened)
        {
            ApplyUnlockedState();
        }
    }

    public void Interact()
    {
        if (drawerObject == null || hasOpened || isSliding) return;

        StartCoroutine(SlideDrawerRoutine());
    }

    private void SetKeyColliderEnabled(bool enabled)
    {
        if (keyCollectible == null) return;

        Collider keyCollider = keyCollectible.GetComponentInChildren<Collider>(true);
        if (keyCollider != null)
        {
            keyCollider.enabled = enabled;
        }
        else
        {
            Debug.LogWarning("DrawerInteractable: key collectible has no Collider.", keyCollectible);
        }
    }

    public bool CanInteractFrom(Transform interactorOrigin)
    {
        if (interactorOrigin == null || drawerObject == null || hasOpened || isSliding)
            return false;

        Transform anchor = interactionAnchor != null ? interactionAnchor : drawerObject.transform;
        Vector3 toInteractor = interactorOrigin.position - anchor.position;

        if (toInteractor.sqrMagnitude > maxInteractDistance * maxInteractDistance)
            return false;

        Vector3 frontDirection = openOffset.sqrMagnitude > 0.0001f
            ? openOffset.normalized
            : -anchor.forward;

        return Vector3.Dot(frontDirection, toInteractor.normalized) >= frontDotThreshold;
    }

    // This handles sliding the drawer physically over time instead of using an Animation Clip
    private IEnumerator SlideDrawerRoutine()
    {
        isSliding = true;
        SetDrawerCollidersEnabled(false);

        // Loop until the drawer reaches the target position
        while (Vector3.Distance(drawerObject.transform.position, openPosition) > 0.001f)
        {
            drawerObject.transform.position = Vector3.MoveTowards(
                drawerObject.transform.position,
                openPosition,
                slideSpeed * Time.deltaTime
            );

            yield return null; // Wait for the next frame
        }

        // Snap exactly to the final position to be safe
        drawerObject.transform.position = openPosition;
        hasOpened = true;
        isSliding = false;
        SetKeyColliderEnabled(true);
    }

    private void ApplyUnlockedState()
    {
        if (drawerObject == null) return;

        // Since this is loading a save file, skip the smooth slide and instantly snap it open
        drawerObject.transform.position = openPosition;
        SetKeyColliderEnabled(true);

        SetDrawerCollidersEnabled(false);
    }

    private void SetDrawerCollidersEnabled(bool enabled)
    {
        foreach (Collider col in GetComponents<Collider>())
            col.enabled = enabled;
    }

    public DrawerSaveData GetSaveData()
    {
        return new DrawerSaveData
        {
            hasDrawerOpened = this.hasOpened
        };
    }

    public void LoadSaveData(DrawerSaveData data)
    {
        if (data == null) return;
        this.hasOpened = data.hasDrawerOpened;

        if (this.hasOpened)
        {
            ApplyUnlockedState();
        }
        else
        {
            SetKeyColliderEnabled(false);
            SetDrawerCollidersEnabled(true);
        }
    }
}
