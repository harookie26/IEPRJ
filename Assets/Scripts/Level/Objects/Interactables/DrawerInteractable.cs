using Game.ObjectTypes;
using System.Collections;
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

    [Header("Key Reveal")]
    [Tooltip("Fallback reveal distance when drawer mesh bounds cannot be determined.")]
    [SerializeField, Min(0f)] private float keyRevealDistance = 1.0f;
    [Tooltip("Vertical clearance above the opened drawer mesh.")]
    [SerializeField, Min(0f)] private float keyFrontInset = 0.04f;

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
    private Vector3 keyClosedLocalPosition;

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

        if (keyCollectible != null)
            keyClosedLocalPosition = keyCollectible.transform.localPosition;

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

        Collider[] keyColliders = keyCollectible.GetComponentsInChildren<Collider>(true);
        if (keyColliders.Length > 0)
        {
            foreach (Collider keyCollider in keyColliders)
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

            float openProgress = Mathf.InverseLerp(
                0f,
                Vector3.Distance(closedPosition, openPosition),
                Vector3.Distance(closedPosition, drawerObject.transform.position));
            ApplyKeyReveal(openProgress);

            yield return null; // Wait for the next frame
        }

        // Snap exactly to the final position to be safe
        drawerObject.transform.position = openPosition;
        ApplyKeyReveal(1f);
        hasOpened = true;
        isSliding = false;
        SetKeyColliderEnabled(true);
    }

    private void ApplyUnlockedState()
    {
        if (drawerObject == null) return;

        // Since this is loading a save file, skip the smooth slide and instantly snap it open
        drawerObject.transform.position = openPosition;
        ApplyKeyReveal(1f);
        SetKeyColliderEnabled(true);

        SetDrawerCollidersEnabled(false);
    }

    private void ApplyKeyReveal(float progress)
    {
        if (keyCollectible == null || drawerObject == null)
            return;

        Vector3 outward = openOffset.sqrMagnitude > 0.0001f
            ? openOffset.normalized
            : -drawerObject.transform.forward;
        Vector3 basePosition = drawerObject.transform.TransformPoint(keyClosedLocalPosition);
        float revealDistance = keyRevealDistance;

        if (TryGetDrawerBounds(out Bounds drawerBounds))
        {
            Vector3 extents = drawerBounds.extents;
            float projectedExtent =
                Mathf.Abs(outward.x) * extents.x
                + Mathf.Abs(outward.y) * extents.y
                + Mathf.Abs(outward.z) * extents.z;
            Vector3 visiblePosition = drawerBounds.center + outward * (projectedExtent * 0.9f);
            visiblePosition.y = drawerBounds.max.y + keyFrontInset;

            keyCollectible.transform.position = Vector3.Lerp(
                basePosition,
                visiblePosition,
                Mathf.Clamp01(progress));
            return;
        }

        keyCollectible.transform.position = Vector3.Lerp(
            basePosition,
            basePosition + outward * revealDistance,
            Mathf.Clamp01(progress));
    }

    private bool TryGetDrawerBounds(out Bounds bounds)
    {
        bounds = default;
        bool foundRenderer = false;

        foreach (Renderer renderer in drawerObject.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || renderer.transform.IsChildOf(keyCollectible.transform))
                continue;

            if (!foundRenderer)
            {
                bounds = renderer.bounds;
                foundRenderer = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return foundRenderer;
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
            if (keyCollectible != null)
                keyCollectible.transform.localPosition = keyClosedLocalPosition;
            SetKeyColliderEnabled(false);
            SetDrawerCollidersEnabled(true);
        }
    }
}
