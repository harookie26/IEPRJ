using System.Collections;
using UnityEngine;
using static EventNames.GameStateEvents;

public class CheckpointPhases : MonoBehaviour
{
    // Use Cache for performance instead of repetitive Find calls
    private GameObject _player;
    private EnemyStateMachine _enemyStateMachine;
    private PlayerMovement _playerMovement;
    private PBController _pbController;
    private ScreenFader _screenFader;
    private UIManager _uiManager;
    private Flashlight _flashlight;
    private CorruptPaintingRandomizer _paintingRandomizer;
    private CorruptedPaintingTutorial _tutorialPainting;
    private CharacterController _playerCC;
    private MainPainting _mainPainting;

    [Header("Object References")]
    [SerializeField] private GameObject _door;
    [SerializeField] private GameObject _key;
    [SerializeField] private GameObject _paintbucket;

    [Header("Spawn Settings")]
    [SerializeField] private Vector3[] _spawnPoints;

    private int _currentPhase;

    private void Awake()
    {
        _player = GameObject.FindWithTag("Player");
        if (_player != null) _playerCC = _player.GetComponent<CharacterController>();

        _enemyStateMachine = GameObject.FindWithTag("Enemy")?.GetComponent<EnemyStateMachine>();
        _playerMovement = FindFirstObjectByType<PlayerMovement>();
        _pbController = FindFirstObjectByType<PBController>();
        _screenFader = FindFirstObjectByType<ScreenFader>();
        _uiManager = FindFirstObjectByType<UIManager>();
        _flashlight = FindFirstObjectByType<Flashlight>();
        _paintingRandomizer = FindFirstObjectByType<CorruptPaintingRandomizer>();
        _tutorialPainting = FindFirstObjectByType<CorruptedPaintingTutorial>();
        _mainPainting = FindFirstObjectByType<MainPainting>();
    }

    private void Start()
    {
        _currentPhase = 0;

        // FIX: Use .position (World Space) to match how MoveToPhase works
        if (_spawnPoints.Length > 0 && _player != null)
        {
            _spawnPoints[0] = _player.transform.position;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            int nextPhase = (_currentPhase + 1) % _spawnPoints.Length;
            SwitchToPhase(nextPhase);
        }
    }

    private void SetPlayerPosition(int phase)
    {
        if (phase < 0 || phase >= _spawnPoints.Length) return;

        if (_playerCC != null)
            _playerCC.enabled = false;

        // Painting phases
        if (phase >= 5 && phase < 9)
        {
            int paintingIndex = phase - 5;

            if (paintingIndex < 0 || paintingIndex >= 4)
            {
                Debug.LogError($"Invalid painting index: {paintingIndex}");
                return;
            }

            GameObject painting = _paintingRandomizer.GetActiveCorruptedPaintings()[paintingIndex];

            Transform t = _paintingRandomizer.GetPaintingSpawnTransform(paintingIndex);

            if (t != null)
            {
                Vector3 dir = t.up;
                dir.y = 0f;
                dir.Normalize();

                Vector3 spawnPos = t.position + (-dir * 2.0f);

                Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

                _player.transform.SetPositionAndRotation(spawnPos, rot);

                Debug.Log($"[Checkpoint] Teleported to Painting {paintingIndex}");
                return;
            }
        }

        // Normal phases
        _player.transform.position = _spawnPoints[phase];

        if (phase == 9)
        {
            Vector3 spawnPos = new Vector3(7.01900005f, 9.03999996f, 1.36099994f);

            _player.transform.position = spawnPos;
        }

        Rigidbody rb = _player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void AdjustGameState(int phase)
    {
        Time.timeScale = 1f;
        EventBroadcaster.Instance.PostEvent(ON_GAME_RESUME);

        // We handle position inside the Coroutine now to ensure CC is disabled correctly
        _flashlight.SetIsOn(false);

        // Key Logic
        if (phase == 0)
        {
            PlayerCollectibleManager.Instance.RemoveCollected("Key");
            if (_key) _key.SetActive(true);
        }
        else
        {
            PlayerCollectibleManager.Instance.AddCollected("Key");
            if (_key) _key.SetActive(false);
        }

        // Door Logic
        if (_door) _door.SetActive(phase < 2);

        // Paintbucket Logic
        if (phase >= 3)
        {
            PlayerCollectibleManager.Instance.AddCollected("Paintbucket");
            if (_paintbucket) _paintbucket.SetActive(false);
        }
        else
        {
            PlayerCollectibleManager.Instance.RemoveCollected("Paintbucket");
            if (_paintbucket) _paintbucket.SetActive(true);
        }

        if (_tutorialPainting != null) _tutorialPainting.ApplyCheckpointPhase(phase);

        // Enemy Logic
        if (_enemyStateMachine != null)
        {
            if (phase >= 5) _enemyStateMachine.ReactivateEnemyState();
            else _enemyStateMachine.ResetEnemyState();
        }

        if (_paintingRandomizer != null) _paintingRandomizer.ApplyCheckpointPhase(phase);

        if (_mainPainting != null)
        {
            _mainPainting.paint1Done = phase >= 5;
            _mainPainting.paint2Done = phase >= 6;
            _mainPainting.paint3Done = phase >= 7;
            _mainPainting.paint4Done = phase >= 8;
        }
    }

    public void SwitchToPhase(int phase)
    {
        if (phase < 0 || phase >= _spawnPoints.Length) return;
        StartCoroutine(SwitchingCoroutine(phase));
    }

    private IEnumerator SwitchingCoroutine(int phase)
    {
        _currentPhase = phase;
        _playerMovement.SetCanMove(false);

        if (_pbController != null) _pbController.OnGameRestartReset();

        yield return StartCoroutine(_screenFader.FadeOutSequence(0.5f));

        if (_uiManager != null)
        {
            _uiManager.HideAll();
            _uiManager.ShowRespawnHUD();
        }

        // 1. STALL PHYSICS / CC
        if (_playerCC != null) _playerCC.enabled = false;

        Rigidbody playerRb = _player.GetComponent<Rigidbody>();
        if (playerRb != null) playerRb.isKinematic = true;

        // 2. MOVE AND ADJUST STATE
        SetPlayerPosition(phase);
        AdjustGameState(phase);

        // 3. WAIT A FRAME 
        // This is crucial for the CharacterController to register the new position
        yield return new WaitForFixedUpdate();
        yield return null;

        // 4. RE-ENABLE
        if (playerRb != null)
        {
            playerRb.isKinematic = false;
            playerRb.linearVelocity = Vector3.zero;
        }

        if (_playerCC != null) _playerCC.enabled = true;
        if (_uiManager != null) _uiManager.HideAll();

        yield return StartCoroutine(_screenFader.FadeInSequence(0.5f));
        _playerMovement.SetCanMove(true);

        Debug.Log($"Moved to Phase {phase} at {_spawnPoints[phase]}");
    }
}