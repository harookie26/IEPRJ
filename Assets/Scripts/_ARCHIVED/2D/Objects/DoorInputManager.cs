using UnityEngine;
using System.Linq;
using System.Collections;
using static EventNames;

public class DoorInputManager : MonoBehaviour
{
    private bool _isCutsceneActive = false;
    private GameObject _player;
    private PlayerMovement _playerMovement;

    [Header("Door Cooldown")]
    [Tooltip("Seconds after initiating a door transfer before another can be started.")] 
    public float doorUseCooldown = 3f; // made public for other components
    private float _lastDoorUseTime = -Mathf.Infinity;
    private bool _isTransferring = false;

    // Public read-only access to last use time
    public float LastDoorUseTime => _lastDoorUseTime;

    private ScreenFader screenFader => FindFirstObjectByType<ScreenFader>();
    private EnemyStateMachine enemy => FindFirstObjectByType<EnemyStateMachine>();

    private void Awake()
    {
        EventBroadcaster.Instance.AddObserver(CutsceneEvents.CUTSCENE_START, () => _isCutsceneActive = true);
        EventBroadcaster.Instance.AddObserver(CutsceneEvents.CUTSCENE_END, () => _isCutsceneActive = false);

        _player = GameObject.FindGameObjectWithTag("Player");
        _playerMovement = FindFirstObjectByType<PlayerMovement>();
    }

    private void Update()
    {
        if (_isCutsceneActive || _player == null)
            return;

        // Early exit if actively transferring
        if (_isTransferring)
            return;

        // Early exit if in cooldown
        if (Time.unscaledTime < _lastDoorUseTime + doorUseCooldown)
            return;

        // Only process if interact was pressed THIS frame
        if (InputManager.Instance.WasInteractPressed())
        {
            // Check if player is in a valid door zone and door is ready
            if (DoorsComponent.CurrentDoor != null && DoorsComponent.CurrentDoor.IsReadyToUse())
            {
                _lastDoorUseTime = Time.unscaledTime;
                StartCoroutine(TransferPlayer(0.05f));
            }
        }
    }

    private IEnumerator TransferPlayer(float postFadeDelaySeconds)
    {
        _isTransferring = true;

        if (screenFader == null)
        {
            Debug.LogWarning("[DoorInputManager] ScreenFader not found. Moving immediately.");
            DoorsComponent.CurrentDoor.MoveToLinkedDoor();
            _isTransferring = false;
            yield break;
        }

        _playerMovement.SetCanMove(false);
        enemy.Freeze();
        yield return StartCoroutine(screenFader.FadeOutSequence(0.25f));

        if (postFadeDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(postFadeDelaySeconds);

        DoorsComponent.CurrentDoor.MoveToLinkedDoor();

        yield return StartCoroutine(screenFader.FadeInSequence(0.25f));
        enemy.Unfreeze();
        _playerMovement.SetCanMove(true);
        _isTransferring = false;
    }
}