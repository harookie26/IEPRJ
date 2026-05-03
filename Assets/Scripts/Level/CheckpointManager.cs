using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[FoldableInspector]
public class CheckpointManager : MonoBehaviour
{
    private int _currentCheckpointIndex;

    private GameStateManager _gameState => FindFirstObjectByType<GameStateManager>();

    private GameObject _player => GameObject.FindWithTag("Player");
    private Vector3 _playerSavedPosition;
    private Vector3 _playerSavedRotation;
    private PlayerMovement _playerMovement => FindFirstObjectByType<PlayerMovement>();

    private GameObject _enemy;
    private Vector3 _enemySavedPosition;
    private EnemyStateMachine.EnemyStateType _enemySavedState;
    private Vector3 _enemySavedDistractedPaintingPosition;
    private bool _enemySavedWasDistracted;
    private string _enemySavedStateName;
    private HashSet<string> _savedCompletedDialogues;

    private EnemyStateMachine _enemyStateMachine => FindFirstObjectByType<EnemyStateMachine>();

    private ScreenFader _screenFader => FindFirstObjectByType<ScreenFader>();
    private UIManager _uiManager => FindFirstObjectByType<UIManager>();

    private void Awake()
    {
        _enemy = GameObject.FindWithTag("Enemy");
    }

    private void Start()
    {
        _currentCheckpointIndex = 0;
        SaveCheckpoint();
    }

    public void SaveCheckpoint()
    {
        _currentCheckpointIndex = _gameState.currentLevelProgress;

        _playerSavedPosition = _player.transform.position;
        _playerSavedRotation = _player.transform.eulerAngles;

        _enemySavedPosition = _enemy.transform.position;

        if (_enemyStateMachine != null)
        {
            _enemySavedState = _enemyStateMachine.CurrentStateType;

            var paintingPos = _enemyStateMachine.GetDistractedPaintingPosition();
            if (paintingPos.HasValue)
            {
                _enemySavedWasDistracted = true;
                _enemySavedDistractedPaintingPosition = paintingPos.Value;
            }
            else
            {
                _enemySavedWasDistracted = false;
                _enemySavedDistractedPaintingPosition = Vector3.zero;
            }

            _enemySavedStateName = _enemyStateMachine.CurrentStateName;
        }
        else
        {
            _enemySavedState = EnemyStateMachine.EnemyStateType.Unknown;
            _enemySavedWasDistracted = false;
            _enemySavedDistractedPaintingPosition = Vector3.zero;
            _enemySavedStateName = "NoEnemyStateMachine";
        }

        _savedCompletedDialogues = new HashSet<string>(DialogueManager.Instance.GetCompletedDialogues());

        Debug.Log($"[CheckpointManager] Checkpoint saved at index {_currentCheckpointIndex}. PlayerPos: {_playerSavedPosition}, EnemyPos: {_enemySavedPosition}, EnemyState: {_enemySavedStateName}");
    }

    public void StartReturnToCheckpoint()
    {
        StartCoroutine(ReturnToCheckpoint());
    }

    public IEnumerator ReturnToCheckpoint()
    {
        _playerMovement.SetCanMove(false);
        yield return StartCoroutine(_screenFader.FadeOutSequence(0.5f));

        // Show respawn HUD while waiting for _player input
        if (_uiManager != null)
        {
            _uiManager.HideAll();
            _uiManager.ShowRespawnHUD();
        }

        _player.transform.position = _playerSavedPosition;
        _player.transform.eulerAngles = _playerSavedRotation;

        if (_savedCompletedDialogues != null)
        {
            DialogueManager.Instance.RestoreCompletedDialogues(_savedCompletedDialogues);

            EventBroadcaster.Instance.PostEvent(EventNames.GameStateEvents.ON_LEVEL_RELOAD);
        }

        if (_enemy == null)
        {
            // Hide all HUDs if no _enemy or when aborting
            if (_uiManager != null) _uiManager.HideAll();
            yield break;
        }

        if (_enemyStateMachine == null)
        {
            if (_uiManager != null) _uiManager.HideAll();
            yield break;
        }

        _enemy.transform.position = _enemySavedPosition;

        NavMeshAgent agent = _enemyStateMachine.NavAgent;
        if (agent != null)
        {
            try
            {
                agent.Warp(_enemySavedPosition);
                agent.ResetPath();
            }
            catch
            {
                try
                {
                    bool prevEnabled = agent.enabled;
                    agent.enabled = false;
                    _enemy.transform.position = _enemySavedPosition;
                    agent.enabled = prevEnabled;
                }
                catch
                {
                }
            }
        }

        yield return null;

        if (agent != null)
            agent.ResetPath();

        switch (_enemySavedState)
        {
            case EnemyStateMachine.EnemyStateType.Calm:
                _enemyStateMachine.Switchstate(_enemyStateMachine.EnemyCalm);
                break;

            case EnemyStateMachine.EnemyStateType.Chasing:
                _enemyStateMachine.Switchstate(_enemyStateMachine.EnemyChasing);
                break;

            case EnemyStateMachine.EnemyStateType.Distracted:
                if (_enemySavedWasDistracted)
                {
                    _enemyStateMachine.DistractAt(_enemySavedDistractedPaintingPosition);
                }
                else
                {
                    _enemyStateMachine.Switchstate(_enemyStateMachine.EnemyDistracted);
                }
                break;

            case EnemyStateMachine.EnemyStateType.Teleporting:
                _enemyStateMachine.Switchstate(_enemyStateMachine.EnemyTeleporting);
                break;

            default:
                _enemyStateMachine.Switchstate(_enemyStateMachine.EnemyCalm);
                break;
        }

        // Wait for _player input before fading back in
        if (InputManager.Instance != null)
        {
            bool gotInput = false;
            yield return StartCoroutine(InputManager.Instance.WaitForInputCoroutine(() => gotInput = true));

            // hide all HUDs once input is received
            if (_uiManager != null) _uiManager.HideAll();
        }
        else
        {
            // fallback small delay if no InputManager
            yield return new WaitForSecondsRealtime(0.05f);
            if (_uiManager != null) _uiManager.HideAll();
        }

        yield return StartCoroutine(_screenFader.FadeInSequence(0.5f));
        _playerMovement.SetCanMove(true);

        Debug.Log($"[CheckpointManager] Returned to checkpoint at index {_currentCheckpointIndex}. PlayerPos: {_playerSavedPosition}, EnemyPos: {_enemySavedPosition}, EnemyState: {_enemySavedStateName}");
    }
}