using System.Collections;
using UnityEngine;
using static EventNames;

public class StairsInputManager : MonoBehaviour
{
    private bool _isCutsceneActive = false;
    private GameObject _player;
    private PlayerMovement _playerMovement;

    [Header("Stair Cooldown")]
    [Tooltip("Seconds after initiating a stair transfer before another can be started.")]
    public float doorUseCooldown = 3f;
    private float _lastDoorUseTime = -Mathf.Infinity;
    private bool _isTransferring = false;
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

    private void OnDisable()
    {
        EventBroadcaster.Instance.RemoveActionAtObserver(CutsceneEvents.CUTSCENE_START, () => _isCutsceneActive = true);
        EventBroadcaster.Instance.RemoveActionAtObserver(CutsceneEvents.CUTSCENE_END, () => _isCutsceneActive = false);
    }

    private void Update()
    {
        if (_isCutsceneActive || _player == null)
            return;

        if (_isTransferring)
            return;

        if (Time.unscaledTime < _lastDoorUseTime + doorUseCooldown)
            return;

        bool interactThisFrame = InputManager.Instance.WasInteractPressed();
        if (interactThisFrame)
        {
            StairsComponent doorToUse = StairsComponent.CurrentDoor;

            if (doorToUse == null)
            {
                Debug.Log("[StairsInputManager] ❌ Interact pressed but CurrentDoor is NULL");
                return;
            }

            if (doorToUse.IsReadyToUse())
            {
                _lastDoorUseTime = Time.unscaledTime;
                StartCoroutine(TransferPlayer(doorToUse, 0.05f));
            }
            else
            {
                Debug.Log($"[StairsInputManager] ❌ Door {doorToUse.name} not ready for use (player not grounded or invalid state).");
            }
        }
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

        if (screenFader == null)
        {
            Debug.LogWarning("[StairsInputManager] ScreenFader not found. Moving immediately without fade.");
            doorToUse.MoveToLinkedDoor();
            _isTransferring = false;
            yield break;
        }

        if (_playerMovement == null)
        {
            Debug.LogError("[StairsInputManager] PlayerMovement is null! Cannot proceed with transfer.");
            _isTransferring = false;
            yield break;
        }

        if (enemy == null)
        {
            Debug.LogError("[StairsInputManager] EnemyStateMachine is null! Cannot proceed with transfer.");
            _isTransferring = false;
            yield break;
        }

        _playerMovement.SetCanMove(false);
        if(enemy.isEnemyActivated)
        {
            enemy.Freeze();
        }   
        yield return StartCoroutine(screenFader.FadeOutSequence(0.25f));
            
        if (postFadeDelaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(postFadeDelaySeconds);
        }

        if (doorToUse != null)
        {
            doorToUse.MoveToLinkedDoor();
        }
        else
        {
            Debug.LogError("[StairsInputManager] Door reference lost during transfer sequence. Teleport aborted.");
        }

        yield return StartCoroutine(screenFader.FadeInSequence(0.25f));

        if (enemy.isEnemyActivated)
        {
            enemy.Unfreeze();
        }
        _playerMovement.SetCanMove(true);
        _isTransferring = false;
    }
}