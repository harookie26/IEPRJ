using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[FoldableInspector]
public class CheckpointManager : MonoBehaviour
{
    private int _currentCheckpointIndex;
    private bool _respawnInProgress;
    public bool IsRespawnInProgress => _respawnInProgress;

    [SerializeField] private bool forceRespawnAtLobby = true;
    [SerializeField] private Transform playerSpawnPoint;

    private GameStateManager _gameState => FindFirstObjectByType<GameStateManager>();

    private GameObject _player => GameObject.FindWithTag("Player");
    private Vector3 _playerSavedPosition;
    private Vector3 _playerSavedRotation;
    private float _playerSavedCameraPitch;
    private Vector3 _initialPlayerPosition;
    private Vector3 _initialPlayerRotation;
    private PlayerMovement _playerMovement => FindFirstObjectByType<PlayerMovement>();
    private PBController _pbController => FindFirstObjectByType<PBController>();

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
        GameStateManager gameState = _gameState;
        if (gameState != null)
            _currentCheckpointIndex = gameState.currentLevelProgress;
        else
            Debug.LogWarning("[CheckpointManager] GameStateManager not found. Keeping the current checkpoint index.");

        GameObject player = _player;
        if (player == null)
        {
            Debug.LogWarning("[CheckpointManager] Player not found. Checkpoint was not updated.");
            return;
        }

        _playerSavedPosition = player.transform.position;
        _playerSavedRotation = player.transform.eulerAngles;
        _playerSavedCameraPitch = _playerMovement != null ? _playerMovement.CameraPitch : 0f;

        if (_enemy == null)
            _enemy = GameObject.FindWithTag("Enemy");

        if (_enemy != null)
        {
            _enemySavedPosition = _enemy.transform.position;
        }
        else
        {
            Debug.LogWarning("[CheckpointManager] Enemy not found. Saving player checkpoint without updating enemy position.");
        }

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

        // _savedCompletedDialogues = new HashSet<string>(DialogueManager.Instance.GetCompletedDialogues());

        Debug.Log($"[CheckpointManager] Checkpoint saved at index {_currentCheckpointIndex}. PlayerPos: {_playerSavedPosition}, EnemyPos: {_enemySavedPosition}, EnemyState: {_enemySavedStateName}");
    }

    public void StartReturnToCheckpoint()
    {
        ReturnToCheckpoint();
    }

    public void ReturnToCheckpoint() // respawn logic
    {
        if (_respawnInProgress)
        {
            return;
        }

        _respawnInProgress = true;
        EnemyStateMachine.StopAllChaseAudio();

        if (forceRespawnAtLobby)
        {
            StartCoroutine(ReturnToInitialSpawn());
        }
        else
        {
            StartCoroutine(ReturnToCheckpointInternal());
        }

        //yield return StartCoroutine(ReturnToCheckpointInternal());
    }

    private IEnumerator ReturnToInitialSpawn()
    {
        GameObject player = _player;

        // 1. Freeze movement and fade to black
        _pbController.OnGameRestartReset();
        _playerMovement.SetCanMove(false);
        yield return StartCoroutine(_screenFader.FadeOutSequence(0.5f));

        if (_uiManager != null)
        {
            _uiManager.HideAll();
            // You can still show a brief respawn indicator if you like, 
            // or just comment this out if it's no longer needed without input
            _uiManager.ShowRespawnHUD();
        }

        // 2. Disable CharacterController to prevent physics rubber-banding
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 3. Reset Rigidbody velocities and physics state
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.isKinematic = true;
        }

        // 4. Instantly teleport the player using the spawn point's saved pose
        ApplyPlayerSpawnPose(
            playerSpawnPoint.position,
            playerSpawnPoint.rotation.eulerAngles.y,
            Mathf.DeltaAngle(0f, playerSpawnPoint.rotation.eulerAngles.x));

        // 5. Reset enemy position and NavMesh pathing
        if (_enemy != null)
        {
            _enemy.transform.position = _enemySavedPosition;
            NavMeshAgent agent = _enemyStateMachine?.NavAgent;
            if (agent != null)
            {
                try { agent.Warp(_enemySavedPosition); agent.ResetPath(); }
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

        // 6. Wait a frame for Unity physics to catch up to the new position
        yield return new WaitForFixedUpdate();

        // 7. Restore physics state & clean up UI
        if (playerRb != null)
        {
            playerRb.isKinematic = false;
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        if (cc != null) cc.enabled = true;

        if (_uiManager != null) _uiManager.HideAll();

        // 8. Fade back in and restore control
        yield return StartCoroutine(_screenFader.FadeInSequence(0.5f));
        _playerMovement.SetCanMove(true);
        EnemyStateMachine.StopAllChaseAudio();
        AudioList.Current?.StopSurpriseEncounterSFX();
        _respawnInProgress = false;

        Debug.Log($"[CheckpointManager] Instant respawn completed. PlayerPos: {player.transform.position}");
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
            //DialogueManager.Instance.RestoreCompletedDialogues(_savedCompletedDialogues);

            EventBroadcaster.Instance.PostEvent(EventNames.GameStateEvents.ON_LEVEL_RELOAD);
        }

        Vector3 respawnPosition = _playerSavedPosition;
        Vector3 respawnRotation = _playerSavedRotation;

        Debug.Log($"[CheckpointManager] Respawning at checkpoint position: {respawnPosition}, rotation: {respawnRotation}");

        ApplyPlayerSpawnPose(respawnPosition, respawnRotation.y, _playerSavedCameraPitch);

        Debug.Log($"[CheckpointManager] Player position after respawn: {_player.transform.position}");

        if (_enemy == null)
        {
            // Hide all HUDs if no _enemy or when aborting
            if (_uiManager != null) _uiManager.HideAll();
            EnemyStateMachine.StopAllChaseAudio();
            AudioList.Current?.StopSurpriseEncounterSFX();
            _respawnInProgress = false;
            yield break;
        }

        if (_enemyStateMachine == null)
        {
            if (_uiManager != null) _uiManager.HideAll();
            EnemyStateMachine.StopAllChaseAudio();
            AudioList.Current?.StopSurpriseEncounterSFX();
            _respawnInProgress = false;
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
        EnemyStateMachine.StopAllChaseAudio();
        AudioList.Current?.StopSurpriseEncounterSFX();
        _respawnInProgress = false;

        Debug.Log($"[CheckpointManager] Returned to checkpoint at index {_currentCheckpointIndex}. PlayerPos: {_playerSavedPosition}, EnemyPos: {_enemySavedPosition}, EnemyState: {_enemySavedStateName}");
    }

    private void ApplyPlayerSpawnPose(Vector3 position, float yaw, float pitch)
    {
        Quaternion bodyRotation = Quaternion.Euler(0f, yaw, 0f);

        if (_playerMovement != null)
        {
            _playerMovement.TeleportToPose(position, bodyRotation);
            _playerMovement.SetViewRotation(yaw, pitch);
            return;
        }

        _player.transform.SetPositionAndRotation(position, bodyRotation);
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
