using Game.Level;
using System.Collections;
using UnityEngine;

public enum VerticalDoorDirection
{
    None,
    Up,
    Down
}

[FoldableInspector]
public class StairsComponent : MonoBehaviour, IStair
{
    [SerializeField] private StairsComponent partnerDoor;

    private IRoom roomA;
    private IRoom roomB;

    private Collider triggerZone;
    private GameObject _player;

    // Vertical direction setting (Up/Down/None)
    [Header("Vertical Teleport Settings")]
    [SerializeField] private VerticalDoorDirection verticalDirection = VerticalDoorDirection.None;

    [Header("Animation Setup")]
    [SerializeField] private GameObject model;
    [SerializeField] bool hasAnimation = false;
    [SerializeField] bool disableAfterAnimation = false;
    [SerializeField] private Animator modelAnimator;
    [SerializeField] private string animationStateName = "TriggerAnimation";

    private Transform leftDoor;
    private Transform rightDoor;
    private Vector3 leftDoorInitialPosition;
    private Quaternion leftDoorInitialRotation;
    private Vector3 leftDoorInitialScale;
    private Vector3 rightDoorInitialPosition;
    private Quaternion rightDoorInitialRotation;
    private Vector3 rightDoorInitialScale;
    private bool hasInitialDoorPose;

    public VerticalDoorDirection VerticalDirection => verticalDirection;

    private bool _playerInZone = false;

    public bool isInaccesibleOnGameStart = false;

    public static StairsComponent CurrentDoor;
    public int Id => GetInstanceID();
    public IRoom RoomA => roomA;
    public IRoom RoomB => roomB;

    [SerializeField] private float weight = 1f;
    public float Weight => weight;

    private UIManager uiManager;
    private StairsInputManager _doorInputManager;
    private PlayerMovement _playerMovement;

    private void Awake()
    {
        if (partnerDoor == null)
        {
            Debug.LogError($"[Door:{name}] Missing partner door reference.");
            return;
        }

        if (hasAnimation && modelAnimator == null && model != null)
        {
            modelAnimator = model.GetComponent<Animator>();
        }

        if (hasAnimation)
        {
            modelAnimator = ResolveAnimationRootAnimator();
            CacheInitialDoorPose();
        }

        _player = GameObject.FindGameObjectWithTag("Player");
        uiManager = FindFirstObjectByType<UIManager>();
        _doorInputManager = FindFirstObjectByType<StairsInputManager>();
        _playerMovement = FindFirstObjectByType<PlayerMovement>();

        triggerZone = GetComponent<Collider>() ?? triggerZone;

    }

    private void Update()
    {
        if (isInaccesibleOnGameStart)
        {
            if (PlayerCollectibleManager.Instance.HasCollected("Paintbucket"))
            {
                isInaccesibleOnGameStart = false;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInZone = true;
            CurrentDoor = this;
            ShowDoorHUD();
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && CurrentDoor == this)
        {
            _playerInZone = false;
            CurrentDoor = null;
            uiManager?.ClearForcedHUD();
        }
    }

    private void ShowDoorHUD()
    {
        if (uiManager == null) return;

        if (_doorInputManager != null)
        {
            bool cooldownActive = Time.unscaledTime < _doorInputManager.LastDoorUseTime + _doorInputManager.doorUseCooldown;
            if (cooldownActive)
            {
                uiManager.ClearForcedHUD();
                return;
            }
        }

        switch (verticalDirection)
        {
            case VerticalDoorDirection.Up:
                uiManager.ShowHUDForce(UIManager.Keys.StairUp);
                break;
            case VerticalDoorDirection.Down:
                uiManager.ShowHUDForce(UIManager.Keys.StairDown);
                break;
            case VerticalDoorDirection.None:
            default:
                uiManager.ClearForcedHUD();
                break;
        }
    }

    public bool IsReadyToUse()
    {
        if (!_playerInZone)
        {
            Debug.Log($"[Stairs:{name}] Player not in zone.");
            return false;
        }

        if (_playerMovement == null)
        {
            Debug.LogError($"[Stairs:{name}] PlayerMovement reference is NULL!");
            return false;
        }

        return true;
    }

    public void MoveToLinkedDoor()
    {
        if (partnerDoor == null)
        {
            Debug.LogError($"[Stairs:{name}] Partner door not assigned. Teleport failed.");
            return;
        }

        if (_player == null)
        {
            Debug.LogError($"[Stairs:{name}] Player reference is null. Teleport failed.");
            return;
        }

        Vector3 playerPosBefore = _player.transform.position;
        Vector3 targetPos = partnerDoor.transform.position;

        Rigidbody playerRb = _player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.MovePosition(targetPos);
        }
        else
        {
            _player.transform.position = targetPos;
        }

        Vector3 playerPosAfter = _player.transform.position;
        Debug.Log($"  - Player position after move: {playerPosAfter}");
        Debug.Log($"  - Position changed: {playerPosBefore != playerPosAfter}");
        Debug.Log($"[Stairs:{name}] Teleported player from {playerPosBefore} to {playerPosAfter}");

        if (_playerMovement != null)
        {
            _playerMovement.ResetVelocity();
        }
        else
        {
            Debug.LogWarning($"[Stairs:{name}] PlayerMovement not found. Could not reset velocity.");
        }
    }

    public Vector3 GetEntryPointFor(IRoom fromRoom)
    {
        Vector3 offset = Vector3.zero;

        if (fromRoom == RoomA)
            offset = (RoomB.Center - RoomA.Center);
        else if (fromRoom == RoomB)
            offset = (RoomA.Center - RoomB.Center);
        else
            Debug.LogWarning($"[Door:{name}] Room {fromRoom?.Id} is not connected to this door.");

        offset = offset.normalized * 0.5f;
        return transform.position + offset;
    }

    public IEnumerator PlayAnimationThenDeactivate()
    {
        if (!hasAnimation)
        {
            yield break;
        }

        if (modelAnimator == null)
        {
            Debug.LogWarning($"[Stairs:{name}] Animation is enabled, but no Animator is assigned.");
            yield break;
        }

        int animationStateHash = GetAnimationStateHash();
        if (!modelAnimator.HasState(0, animationStateHash))
        {
            Debug.LogWarning(
                $"[Stairs:{name}] Animator '{modelAnimator.name}' does not contain state " +
                $"'{animationStateName}' on its base layer.");
            yield break;
        }

        modelAnimator.enabled = true;
        modelAnimator.Play(animationStateHash, 0, 0f);
        modelAnimator.Update(0f);

        float animationLength = modelAnimator.GetCurrentAnimatorStateInfo(0).length;

        yield return new WaitForSeconds(animationLength);

        if (model != null)
        {
            //if(disableAfterAnimation)
            //    model.SetActive(false);
            //else
            //    model.SetActive(true);
        }
    }

    public void ResetAnimationPose()
    {
        if (!hasInitialDoorPose)
        {
            return;
        }

        if (modelAnimator != null)
        {
            modelAnimator.enabled = false;
        }

        RestoreLocalTransform(
            leftDoor,
            leftDoorInitialPosition,
            leftDoorInitialRotation,
            leftDoorInitialScale);

        RestoreLocalTransform(
            rightDoor,
            rightDoorInitialPosition,
            rightDoorInitialRotation,
            rightDoorInitialScale);
    }

    private Animator ResolveAnimationRootAnimator()
    {
        if (modelAnimator == null)
        {
            return null;
        }

        RuntimeAnimatorController controller = modelAnimator.runtimeAnimatorController;
        Transform animationRoot = FindAnimationRoot(modelAnimator.transform);
        if (animationRoot == null || animationRoot == modelAnimator.transform)
        {
            return modelAnimator;
        }

        Animator rootAnimator = animationRoot.GetComponent<Animator>();
        if (rootAnimator == null)
        {
            rootAnimator = animationRoot.gameObject.AddComponent<Animator>();
        }

        if (controller != null)
        {
            rootAnimator.runtimeAnimatorController = controller;
        }

        foreach (Animator childAnimator in animationRoot.GetComponentsInChildren<Animator>(true))
        {
            if (childAnimator != rootAnimator &&
                childAnimator.runtimeAnimatorController == rootAnimator.runtimeAnimatorController)
            {
                childAnimator.enabled = false;
            }
        }

        return rootAnimator;
    }

    private static Transform FindAnimationRoot(Transform start)
    {
        Transform current = start;
        while (current != null)
        {
            if (current.Find("left_door.003") != null &&
                current.Find("right_door.003") != null)
            {
                return current;
            }

            current = current.parent;
        }

        return start;
    }

    private void CacheInitialDoorPose()
    {
        if (modelAnimator == null)
        {
            return;
        }

        Transform animationRoot = FindAnimationRoot(modelAnimator.transform);
        leftDoor = animationRoot.Find("left_door.003");
        rightDoor = animationRoot.Find("right_door.003");

        if (leftDoor == null || rightDoor == null)
        {
            Debug.LogWarning($"[Stairs:{name}] Could not find both animated door transforms.");
            return;
        }

        leftDoorInitialPosition = leftDoor.localPosition;
        leftDoorInitialRotation = leftDoor.localRotation;
        leftDoorInitialScale = leftDoor.localScale;
        rightDoorInitialPosition = rightDoor.localPosition;
        rightDoorInitialRotation = rightDoor.localRotation;
        rightDoorInitialScale = rightDoor.localScale;
        hasInitialDoorPose = true;
    }

    private static void RestoreLocalTransform(
        Transform target,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale)
    {
        if (target == null)
        {
            return;
        }

        target.SetLocalPositionAndRotation(position, rotation);
        target.localScale = scale;
    }

    private int GetAnimationStateHash()
    {
        string statePath = animationStateName.Contains(".")
            ? animationStateName
            : $"Base Layer.{animationStateName}";

        return Animator.StringToHash(statePath);
    }
}
