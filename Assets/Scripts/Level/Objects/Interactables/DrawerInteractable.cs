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
        // Don't let the player spam 'E' while it's already sliding
        if (drawerObject == null || hasOpened || isSliding) return;

        hasOpened = true;

        // Multiple interaction listeners can run during the same input event. Delay
        // activation so a later listener cannot collect the key with this press.
        StartCoroutine(EnableKeyNextFrame());

        // Start the smooth slide animation via code
        StartCoroutine(SlideDrawerRoutine());
    }

    private IEnumerator EnableKeyNextFrame()
    {
        yield return null;

        if (keyCollectible == null) yield break;

        Collider keyCollider = keyCollectible.GetComponent<Collider>();
        if (keyCollider != null)
        {
            keyCollider.enabled = true;
        }
        else
        {
            Debug.LogWarning("DrawerInteractable: key collectible has no Collider.", keyCollectible);
        }
    }

    // This handles sliding the drawer physically over time instead of using an Animation Clip
    private IEnumerator SlideDrawerRoutine()
    {
        isSliding = true;

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
        isSliding = false;

        // DESTROY ALL COLLIDERS ON THE DRAWER
        foreach (Collider col in GetComponents<Collider>())
        {
            Destroy(col);
        }
    }

    private void ApplyUnlockedState()
    {
        if (drawerObject == null) return;

        // Since this is loading a save file, skip the smooth slide and instantly snap it open
        drawerObject.transform.position = openPosition;

        // DESTROY ALL COLLIDERS ON THE DRAWER
        foreach (Collider col in GetComponents<Collider>())
        {
            Destroy(col);
        }
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
    }
}
