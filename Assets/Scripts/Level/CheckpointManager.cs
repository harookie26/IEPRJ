using UnityEngine;
using System.Collections;
using UnityEngine.AI;

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
    private EnemyStateMachine _enemyStateMachine => FindFirstObjectByType<EnemyStateMachine>();

    private ScreenFader screenFader => FindFirstObjectByType<ScreenFader>();


    private void Awake()
    {
        _enemy = GameObject.FindWithTag("Enemy");
    }

    private void Start()
    {
        _currentCheckpointIndex = 0;
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
        
        Debug.Log($"[CheckpointManager] Checkpoint saved at index {_currentCheckpointIndex}. PlayerPos: {_playerSavedPosition}, EnemyPos: {_enemySavedPosition}, EnemyState: {_enemySavedStateName}");
    }

    public IEnumerator ReturnToCheckpoint()
    {
        _playerMovement.SetCanMove(false);
        yield return StartCoroutine(screenFader.FadeOutSequence(0.5f));

        _player.transform.position = _playerSavedPosition;
        _player.transform.eulerAngles = _playerSavedRotation;

        if (_enemy == null)
            yield break;

        if (_enemyStateMachine == null)
            yield break;

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

        yield return new WaitForSecondsRealtime(0.05f);

        yield return StartCoroutine(screenFader.FadeInSequence(0.5f));
        _playerMovement.SetCanMove(true);

        Debug.Log($"[CheckpointManager] Returned to checkpoint at index {_currentCheckpointIndex}. PlayerPos: {_playerSavedPosition}, EnemyPos: {_enemySavedPosition}, EnemyState: {_enemySavedStateName}");
    }
}