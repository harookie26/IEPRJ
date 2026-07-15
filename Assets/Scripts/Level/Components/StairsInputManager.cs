using System.Collections;
using UnityEngine;
using static EventNames;

public class StairsInputManager : MonoBehaviour
{
    public static bool IsTransferInProgress { get; private set; }

    private GameObject _player;
    private PlayerMovement _playerMovement;
    private UIManager _uiManager;
    private AudioList _audioList;
    private AudioSource _audioSource;
    private ElevatorAttackCutscene _elevatorAttack;

    [Header("Stair Cooldown")]
    [Tooltip("Seconds after initiating a stair transfer before another can be started.")]
    public float doorUseCooldown = 3f;
    private float _lastDoorUseTime = -Mathf.Infinity;
    private bool _isTransferring = false;
    public float LastDoorUseTime => _lastDoorUseTime;

    private InputManager _subscribedInputManager;

    private ScreenFader screenFader => FindFirstObjectByType<ScreenFader>();
    private EnemyStateMachine enemy => FindFirstObjectByType<EnemyStateMachine>();

    private void Awake()
    {
        ResolvePlayerReferences();
        _uiManager = FindFirstObjectByType<UIManager>();
        _audioList = FindAnyObjectByType<AudioList>();
        _audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        _elevatorAttack = FindFirstObjectByType<ElevatorAttackCutscene>(FindObjectsInactive.Include)
            ?? GetComponent<ElevatorAttackCutscene>()
            ?? gameObject.AddComponent<ElevatorAttackCutscene>();
    }

    private void OnEnable()
    {
        TrySubscribeInput();
    }

    private void OnDisable()
    {
        UnsubscribeInput();
        IsTransferInProgress = false;
    }

    private void Update()
    {
        // InputManager can be initialized or replaced after this component.
        if (_subscribedInputManager != InputManager.Instance)
        {
            TrySubscribeInput();
        }

        ResolvePlayerReferences();
    }

    private void HandleInteract()
    {
        if (GameState.IsCutsceneActive)
            return;

        ResolvePlayerReferences();
        if (_player == null)
            return;

        if (_isTransferring)
            return;

        if (Time.unscaledTime < _lastDoorUseTime + doorUseCooldown)
            return;

        StairsComponent doorToUse = StairsComponent.CurrentDoor;

        if (doorToUse == null)
        {
            Debug.Log("[StairsInputManager] ❌ Interact pressed but CurrentDoor is NULL");
            return;
        }

        if (doorToUse.isInaccesibleOnGameStart || doorToUse.fullyinaccesible)
        {
            _uiManager?.ClearForcedHUD();
            DialogueTriggerManager.Instance.TriggerInaccessibleAreaDialogue();
            return;
        }

        if (doorToUse.IsReadyToUse())
        {
            _lastDoorUseTime = Time.unscaledTime;

            if (_elevatorAttack.CanPlay(doorToUse))
                StartCoroutine(PlayElevatorAttack(doorToUse));
            else
                StartCoroutine(TransferPlayer(doorToUse, 0.05f));
        }
        else
        {
            Debug.Log($"[StairsInputManager] ❌ Door {doorToUse.name} not ready for use (player not grounded or invalid state).");
        }
    }

    private void TrySubscribeInput()
    {
        UnsubscribeInput();

        if (InputManager.Instance == null)
            return;

        _subscribedInputManager = InputManager.Instance;
        _subscribedInputManager.OnInteractPressed -= HandleInteract;
        _subscribedInputManager.OnInteractPressed += HandleInteract;
    }

    private void UnsubscribeInput()
    {
        if (_subscribedInputManager != null)
        {
            _subscribedInputManager.OnInteractPressed -= HandleInteract;
        }

        _subscribedInputManager = null;
    }

    private IEnumerator TransferPlayer(StairsComponent doorToUse, float postFadeDelaySeconds)
    {
        if (doorToUse == null)
        {
            Debug.LogError("[StairsInputManager] Door reference became null before transfer started.");
            _isTransferring = false;
            yield break;
        }

        _isTransferring = true;
        IsTransferInProgress = true;

        if (screenFader == null)
        {
            Debug.LogWarning("[StairsInputManager] ScreenFader not found. Moving immediately without fade.");
            doorToUse.MoveToLinkedDoor();
            _isTransferring = false;
            IsTransferInProgress = false;
            yield break;
        }

        ResolvePlayerReferences();
        if (_playerMovement == null)
        {
            Debug.LogError("[StairsInputManager] PlayerMovement is null! Cannot proceed with transfer.");
            _isTransferring = false;
            IsTransferInProgress = false;
            yield break;
        }

        //if (enemy == null)
        //{
        //    Debug.LogError("[StairsInputManager] EnemyStateMachine is null! Cannot proceed with transfer.");
        //    _isTransferring = false;
        //    IsTransferInProgress = false;
        //    yield break;
        //}

        if (_audioList != null && _audioList.elevatorSFX != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_audioList.elevatorSFX);
        }

        _playerMovement.SetCanMove(false);

        if (enemy != null && enemy.isEnemyActivated)
        {
            enemy.Freeze();
        }

        yield return StartCoroutine(doorToUse.PlayAnimationThenDeactivate());
        yield return StartCoroutine(screenFader.FadeOutSequence(0.25f));
        doorToUse.ResetAnimationPose();

        if (postFadeDelaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(postFadeDelaySeconds);
        }

        // Keep the elevator clip contained to the pre-teleport transition.
        _audioSource?.Stop();

        if (doorToUse != null)
        {
            doorToUse.MoveToLinkedDoor();

            // PlayerMovement uses Rigidbody interpolation. Keep the screen black
            // until physics has accepted both parts of the teleported pose so the
            // destination rotation is never rendered at the previous position.
            yield return new WaitForFixedUpdate();
            Physics.SyncTransforms();
        }
        else
        {
            Debug.LogError("[StairsInputManager] Door reference lost during transfer sequence. Teleport aborted.");
        }

        yield return StartCoroutine(screenFader.FadeInSequence(0.25f));

        if (enemy != null && enemy.isEnemyActivated)
        {
            enemy.Unfreeze();
        }
        _playerMovement.SetCanMove(true);
        _isTransferring = false;
        IsTransferInProgress = false;
    }

    private IEnumerator PlayElevatorAttack(StairsComponent door)
    {
        _isTransferring = true;
        IsTransferInProgress = true;
        _uiManager?.ClearForcedHUD();

        yield return StartCoroutine(_elevatorAttack.Play(door));

        _isTransferring = false;
        IsTransferInProgress = false;
    }

    private void ResolvePlayerReferences()
    {
        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player");
        }

        if (_playerMovement == null && _player != null)
        {
            _playerMovement = _player.GetComponent<PlayerMovement>();
            _playerMovement ??= _player.GetComponentInChildren<PlayerMovement>(true);
            _playerMovement ??= _player.GetComponentInParent<PlayerMovement>(true);
        }

        _playerMovement ??= FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);

        if (_player == null && _playerMovement != null)
        {
            _player = _playerMovement.gameObject;
        }
    }
}
