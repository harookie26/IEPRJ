using System.Collections;
using UnityEngine;
using static EventNames.GameStateEvents;

public class CheckpointPhases : MonoBehaviour
{
    private GameObject _player => GameObject.FindWithTag("Player");
    private EnemyStateMachine _enemyStateMachine => GameObject.FindWithTag("Enemy").GetComponent<EnemyStateMachine>();
    private PlayerMovement _playerMovement => FindFirstObjectByType<PlayerMovement>();
    private PBController _pbController => FindFirstObjectByType<PBController>();
    private ScreenFader _screenFader => FindFirstObjectByType<ScreenFader>();
    private UIManager _uiManager => FindFirstObjectByType<UIManager>();
    private Flashlight _flashlight => FindFirstObjectByType<Flashlight>();

    [Header("Object References")]
    [SerializeField] private GameObject _door;
    [SerializeField] private GameObject _key;
    [SerializeField] private GameObject _paintbucket;

    [SerializeField] private Vector3[] _spawnPoints;

    private int _currentPhase;

    private void Start()
    {
        _currentPhase = 0;

        // Optional:
        // If you want phase 0 to become the player's initial spawn automatically
        // uncomment this line.

        _spawnPoints[0] = _player.transform.localPosition;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) // Example: Press 'P' to switch phases for testing
        {
            _currentPhase = (_currentPhase + 1) % _spawnPoints.Length; // Cycle through phases
            SwitchToPhase(_currentPhase);
        }
    }

    public void MoveToPhase(int phase)
    {
        if (phase < 0 || phase >= _spawnPoints.Length)
        {
            Debug.LogError("Invalid phase number.");
            return;
        }

        _currentPhase = phase;
        _player.transform.localPosition = _spawnPoints[phase];
    }

    public void AdjustGameState(int phase)
    {
        MoveToPhase(phase);

        switch (phase)
        {
            // Game start
            case 0:
                Time.timeScale = 1f;
                EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME);

                _enemyStateMachine.ResetEnemyState();

                PlayerCollectibleManager.Instance.RemoveCollected("Key");
                PlayerCollectibleManager.Instance.RemoveCollected("Paintbucket");

                _door.SetActive(true);
                _key.SetActive(false);
                _paintbucket.SetActive(false);
                _flashlight.SetIsOn(false);

                break;

            // Key picked up
            case 1:
                Time.timeScale = 1f;
                EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME);

                _enemyStateMachine.ResetEnemyState();

                PlayerCollectibleManager.Instance.AddCollected("Key");
                PlayerCollectibleManager.Instance.RemoveCollected("Paintbucket");

                _door.SetActive(true);
                _key.SetActive(false);
                _paintbucket.SetActive(true);
                _flashlight.SetIsOn(false);

                break;

            // Door unlocked
            case 2:
                Time.timeScale = 1f;
                EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME);

                _enemyStateMachine.ResetEnemyState();

                PlayerCollectibleManager.Instance.AddCollected("Key");
                PlayerCollectibleManager.Instance.RemoveCollected("Paintbucket");

                _door.SetActive(false);
                _key.SetActive(false);
                _paintbucket.SetActive(true);
                _flashlight.SetIsOn(false);

                break;

            // Paintbucket picked up
            case 3:
                Time.timeScale = 1f;
                EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME);

                _enemyStateMachine.ResetEnemyState();

                PlayerCollectibleManager.Instance.AddCollected("Key");
                PlayerCollectibleManager.Instance.AddCollected("Paintbucket");

                _door.SetActive(false);
                _key.SetActive(false);
                _paintbucket.SetActive(false);
                _flashlight.SetIsOn(false);

                break;

            // Painting tutorial done
            case 4:
                Time.timeScale = 1f;
                EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME);

                _enemyStateMachine.ResetEnemyState();

                PlayerCollectibleManager.Instance.AddCollected("Key");
                PlayerCollectibleManager.Instance.AddCollected("Paintbucket");

                _door.SetActive(false);
                _key.SetActive(false);
                _paintbucket.SetActive(false);
                _flashlight.SetIsOn(false);

                break;

            // Corrupted Painting 1 done
            case 5:
                Time.timeScale = 1f;
                EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME);

                _enemyStateMachine.ReactivateEnemyState();

                PlayerCollectibleManager.Instance.AddCollected("Key");
                PlayerCollectibleManager.Instance.AddCollected("Paintbucket");

                _door.SetActive(false);
                _key.SetActive(false);
                _paintbucket.SetActive(false);
                _flashlight.SetIsOn(false);

                break;

            // Corrupted Painting 2 done
            case 6:
                Time.timeScale = 1f;
                EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME);

                _enemyStateMachine.ReactivateEnemyState();

                PlayerCollectibleManager.Instance.AddCollected("Key");
                PlayerCollectibleManager.Instance.AddCollected("Paintbucket");

                _door.SetActive(false);
                _key.SetActive(false);
                _paintbucket.SetActive(false);
                _flashlight.SetIsOn(false);

                break;

            // Corrupted Painting 3 done
            case 7:
                Time.timeScale = 1f;
                EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME);

                _enemyStateMachine.ReactivateEnemyState();

                PlayerCollectibleManager.Instance.AddCollected("Key");
                PlayerCollectibleManager.Instance.AddCollected("Paintbucket");

                _door.SetActive(false);
                _key.SetActive(false);
                _paintbucket.SetActive(false);
                _flashlight.SetIsOn(false);

                break;

            // Corrupted Painting 4 done
            case 8:
                Time.timeScale = 1f;
                EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME);

                _enemyStateMachine.ReactivateEnemyState();

                PlayerCollectibleManager.Instance.AddCollected("Key");
                PlayerCollectibleManager.Instance.AddCollected("Paintbucket");

                _door.SetActive(false);
                _key.SetActive(false);
                _paintbucket.SetActive(false);
                _flashlight.SetIsOn(false);

                break;

            // Main Painting done
            case 9:
                Time.timeScale = 1f;
                EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME);

                _enemyStateMachine.ReactivateEnemyState();

                PlayerCollectibleManager.Instance.AddCollected("Key");
                PlayerCollectibleManager.Instance.AddCollected("Paintbucket");

                _door.SetActive(false);
                _key.SetActive(false);
                _paintbucket.SetActive(false);
                _flashlight.SetIsOn(false);

                break;

            default:
                Debug.LogWarning("No specific logic defined for this phase.");
                break;
        }
    }

    public void SwitchToPhase(int phase)
    {
        if (phase < 0 || phase >= _spawnPoints.Length)
        {
            Debug.LogError("Invalid phase number.");
            return;
        }

        _currentPhase = phase;
        StartCoroutine(SwitchingCoroutine(phase));

    }

    private IEnumerator SwitchingCoroutine(int phase)
    {

        GameObject player = _player;

        _pbController.OnGameRestartReset();
        _playerMovement.SetCanMove(false);
        yield return StartCoroutine(_screenFader.FadeOutSequence(0.5f));

        if (_uiManager != null)
        {
            _uiManager.HideAll();
            _uiManager.ShowRespawnHUD();
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.isKinematic = true;
        }

        MoveToPhase(phase);
        AdjustGameState(phase);

        yield return new WaitForFixedUpdate();

        if (playerRb != null)
        {
            playerRb.isKinematic = false;
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        if (cc != null) cc.enabled = true;

        if (_uiManager != null) _uiManager.HideAll();

        yield return StartCoroutine(_screenFader.FadeInSequence(0.5f));
        _playerMovement.SetCanMove(true);

        Debug.Log($"[CheckpointManager] Instant respawn completed. PlayerPos: {player.transform.position}");
    }
}