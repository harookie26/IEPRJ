using UnityEngine;
using System.Linq;
using System.Collections;
using static EventNames;

public class DoorInputManager : MonoBehaviour
{
    private bool _isCutsceneActive = false;
    private GameObject _player;

    private ScreenFader screenFader => FindFirstObjectByType<ScreenFader>();
    private EnemyStateMachine enemy => FindFirstObjectByType<EnemyStateMachine>();

    private void Awake()
    {
        EventBroadcaster.Instance.AddObserver(CutsceneEvents.CUTSCENE_START, () => _isCutsceneActive = true);
        EventBroadcaster.Instance.AddObserver(CutsceneEvents.CUTSCENE_END, () => _isCutsceneActive = false);
    }

    private void OnEnable()
    {
    }

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        if (_isCutsceneActive || _player == null)
            return;

        if (InputManager.Instance.WasInteractPressed())
        {

            if (DoorsComponent.CurrentDoor?.IsReadyToUse() == true)
            {
                StartCoroutine(TransferPlayer(0.05f));
            }
            else
            {
                Debug.LogWarning("[DoorInputManager] No valid door found.");
            }
        }
    }

    private IEnumerator TransferPlayer(float postFadeDelaySeconds)
    {
        if (screenFader == null)
        {
            Debug.LogWarning("[DoorInputManager] ScreenFader not found. Moving immediately.");
            DoorsComponent.CurrentDoor.MoveToLinkedDoor();
            yield break;
        }

        enemy.Freeze();
        yield return StartCoroutine(screenFader.FadeOutSequence());

        if (postFadeDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(postFadeDelaySeconds);

        DoorsComponent.CurrentDoor.MoveToLinkedDoor();

        yield return StartCoroutine(screenFader.FadeInSequence());
        enemy.Unfreeze();
    }
}