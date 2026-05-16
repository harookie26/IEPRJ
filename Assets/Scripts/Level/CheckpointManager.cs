using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[FoldableInspector]
public class CheckpointManager : MonoBehaviour
{
    private int _currentCheckpointIndex;

    [SerializeField] private bool forceRespawnAtLobby = false;

    private GameStateManager _gameState => FindFirstObjectByType<GameStateManager>();

    private GameObject _player => GameObject.FindWithTag("Player");
    private Vector3 _playerSavedPosition;
    private Vector3 _playerSavedRotation;
    private Vector3 _initialPlayerPosition;
    private Vector3 _initialPlayerRotation;
    private PlayerMovement _playerMovement => FindFirstObjectByType<PlayerMovement>();

    private GameObject _enemy;
    private Vector3 _enemySavedPosition;
    // private EnemyStateMachine.EnemyStateType _enemySavedState;
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
        _initialPlayerPosition = _player.transform.position;
        _initialPlayerRotation = _player.transform.eulerAngles;
        Debug.Log($"[CheckpointManager] Initial spawn position captured: {_initialPlayerPosition}, rotation: {_initialPlayerRotation}");
        SaveCheckpoint();
    }

    public void SaveCheckpoint()
    {
        _currentCheckpointIndex = _gameState.currentLevelProgress;

        _playerSavedPosition = _player.transform.position;
        _playerSavedRotation = _player.transform.eulerAngles;

        _enemySavedPosition = _enemy.transform.position;

        //if (_enemyStateMachine != null)
        //{
        //    _enemySavedState = _enemyStateMachine.CurrentStateType;

        //    var paintingPos = _enemyStateMachine.GetDistractedPaintingPosition();
        //    if (paintingPos.HasValue)
        //    {
        //        _enemySavedWasDistracted = true;
        //        _enemySavedDistractedPaintingPosition = paintingPos.Value;
        //    }
        //    else
        //    {
        //        _enemySavedWasDistracted = false;
        //        _enemySavedDistractedPaintingPosition = Vector3.zero;
        //    }

        //    _enemySavedStateName = _enemyStateMachine.CurrentStateName;
        //}
        //else
        //{
        //    _enemySavedState = EnemyStateMachine.EnemyStateType.Unknown;
        //    _enemySavedWasDistracted = false;
        //    _enemySavedDistractedPaintingPosition = Vector3.zero;
        //    _enemySavedStateName = "NoEnemyStateMachine";
        //}

        _savedCompletedDialogues = new HashSet<string>(DialogueManager.Instance.GetCompletedDialogues());

        Debug.Log($"[CheckpointManager] Checkpoint saved at index {_currentCheckpointIndex}. PlayerPos: {_playerSavedPosition}, EnemyPos: {_enemySavedPosition}, EnemyState: {_enemySavedStateName}");
    }

    public void StartReturnToCheckpoint()
    {
        StartCoroutine(ReturnToCheckpoint());
    }

    public IEnumerator ReturnToCheckpoint()
    {
        if (forceRespawnAtLobby)
        {
            yield return StartCoroutine(ReturnToInitialSpawn());
            yield break;
        }

        yield return StartCoroutine(ReturnToCheckpointInternal());
    }

    private IEnumerator ReturnToInitialSpawn()
    {
        _playerMovement.SetCanMove(false);
        yield return StartCoroutine(_screenFader.FadeOutSequence(0.5f));

        // Show respawn HUD while waiting for _player input
        if (_uiManager != null)
        {
            _uiManager.HideAll();
            _uiManager.ShowRespawnHUD();
        }

        Debug.Log($"[CheckpointManager] Respawning at initial position: {_initialPlayerPosition}, rotation: {_initialPlayerRotation}");

        // Freeze rigidbody to prevent physics from moving the player
        Rigidbody playerRb = _player.GetComponent<Rigidbody>();
        RigidbodyConstraints originalConstraints = RigidbodyConstraints.None;
        if (playerRb != null)
        {
            originalConstraints = playerRb.constraints;
            playerRb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;
        }

        _player.transform.position = _initialPlayerPosition;
        _player.transform.eulerAngles = _initialPlayerRotation;

        // Reset rigidbody velocity
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        // Reset enemy to initial position as well
        if (_enemy != null)
        {
            _enemy.transform.position = _enemySavedPosition;
            NavMeshAgent agent = _enemyStateMachine?.NavAgent;
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
                    catch { }
                }
            }
        }

        // Wait for player input before fading back in
        if (InputManager.Instance != null)
        {
            bool gotInput = false;
            yield return StartCoroutine(InputManager.Instance.WaitForInputCoroutine(() => gotInput = true));
            if (_uiManager != null) _uiManager.HideAll();
        }
        else
        {
            yield return new WaitForSecondsRealtime(0.05f);
            if (_uiManager != null) _uiManager.HideAll();
        }

        // Restore rigidbody constraints
        if (playerRb != null)
        {
            playerRb.constraints = originalConstraints;
        }

        yield return StartCoroutine(_screenFader.FadeInSequence(0.5f));
        _playerMovement.SetCanMove(true);

        Debug.Log($"[CheckpointManager] Respawned at initial spawn. PlayerPos: {_player.transform.position}");
    }

    private IEnumerator ReturnToCheckpointInternal()
    {
        _playerMovement.SetCanMove(false);
        yield return StartCoroutine(_screenFader.FadeOutSequence(0.5f));

        // Show respawn HUD while waiting for _player input
        if (_uiManager != null)
        {
            _uiManager.HideAll();
            _uiManager.ShowRespawnHUD();
        }

        if (_savedCompletedDialogues != null)
        {
            DialogueManager.Instance.RestoreCompletedDialogues(_savedCompletedDialogues);

            EventBroadcaster.Instance.PostEvent(EventNames.GameStateEvents.ON_LEVEL_RELOAD);
        }

        Vector3 respawnPosition = _playerSavedPosition;
        Vector3 respawnRotation = _playerSavedRotation;

        Debug.Log($"[CheckpointManager] Respawning at checkpoint position: {respawnPosition}, rotation: {respawnRotation}");

        _player.transform.position = respawnPosition;
        _player.transform.eulerAngles = respawnRotation;

        Debug.Log($"[CheckpointManager] Player position after respawn: {_player.transform.position}");

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

        //switch (_enemySavedState)
        //{

        //    default:
        //        break;
        //}

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

    public void SetForceRespawnAtStartSpawn(bool force)
    {
        forceRespawnAtLobby = force;
        Debug.Log($"[CheckpointManager] Force respawn at start spawn: {forceRespawnAtLobby}");
    }

    public bool GetForceRespawnAtStartSpawn()
    {
        return forceRespawnAtLobby;
    }
}