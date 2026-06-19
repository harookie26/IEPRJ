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

    [Header("Teleport Exit Settings")]
    [SerializeField, Min(0f)] private float minimumExitDistance = 1.25f;
    [SerializeField, Min(0f)] private float exitClearance = 0.35f;
    [SerializeField, Min(0f)] private float safePositionSearchDistance = 2f;
    [SerializeField, Min(1)] private int safePositionSearchSteps = 8;

    [Header("Animation Setup")]
    [SerializeField] private GameObject model;
    [SerializeField] bool hasAnimation = false;
    [SerializeField] bool disableAfterAnimation = false;
    [SerializeField] private Animator modelAnimator;
    [SerializeField] private string animationStateName = "TriggerAnimation";
    [SerializeField, Min(0.01f)] private float replacementDoorOpenDuration = 1.25f;
    [SerializeField, Min(0.001f)] private float replacementDoorOpenDistance = 0.018f;

    private Transform leftDoor;
    private Transform rightDoor;
    private Vector3 leftDoorInitialPosition;
    private Quaternion leftDoorInitialRotation;
    private Vector3 leftDoorInitialScale;
    private Vector3 rightDoorInitialPosition;
    private Quaternion rightDoorInitialRotation;
    private Vector3 rightDoorInitialScale;
    private bool hasInitialDoorPose;
    private bool useReplacementDoorAnimation;

    public VerticalDoorDirection VerticalDirection => verticalDirection;

    private bool _playerInZone = false;

    public bool isInaccesibleOnGameStart = false;
    public bool fullyinaccesible = false;

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

        ResolvePlayerReferences();
        uiManager = FindFirstObjectByType<UIManager>();
        _doorInputManager = FindFirstObjectByType<StairsInputManager>();

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

        if (!ResolvePlayerReferences())
        {
            Debug.LogError($"[Stairs:{name}] PlayerMovement reference is NULL!");
            return false;
        }

        return true;
    }

    public void MoveToLinkedDoor()
    {
        ResolvePlayerReferences();

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
        Vector3 outward = partnerDoor.GetOutwardDirection();
        Vector3 targetPos = partnerDoor.GetExitPosition(outward);
        Quaternion targetRotation = Quaternion.LookRotation(outward, Vector3.up);

        if (_playerMovement != null &&
            _playerMovement.TryFindSafeTeleportPosition(
                targetPos,
                outward,
                safePositionSearchDistance,
                safePositionSearchSteps,
                out Vector3 safeTargetPos))
        {
            targetPos = safeTargetPos;
        }

        if (_playerMovement != null)
        {
            _playerMovement.TeleportToPose(targetPos, targetRotation);
        }
        else
        {
            _player.transform.SetPositionAndRotation(targetPos, targetRotation);
        }

        Vector3 playerPosAfter = _player.transform.position;
        Debug.Log(
            $"[Stairs:{name}] Teleported player from {playerPosBefore} to {playerPosAfter}, " +
            $"facing outward {outward}.");
    }

    private bool ResolvePlayerReferences()
    {
        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player");
        }

        if (_playerMovement == null && _player != null)
        {
            _playerMovement = _player.GetComponent<PlayerMovement>();
            _playerMovement ??= _player.GetComponentInChildren<PlayerMovement>(true);
            _playerMovement ??= _player.GetComponentInParent<PlayerMovement>(true);
        }

        _playerMovement ??= FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);

        if (_player == null && _playerMovement != null)
        {
            _player = _playerMovement.gameObject;
        }

        return _player != null && _playerMovement != null;
    }

    private Vector3 GetOutwardDirection()
    {
        Vector3 outward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (outward.sqrMagnitude <= 0.0001f)
        {
            outward = Vector3.forward;
        }

        return outward.normalized;
    }

    private Vector3 GetExitPosition(Vector3 outward)
    {
        Collider destinationTrigger = triggerZone != null ? triggerZone : GetComponent<Collider>();
        Vector3 exitOrigin = transform.position;
        float triggerExtent = 0f;

        if (destinationTrigger != null)
        {
            Bounds bounds = destinationTrigger.bounds;
            exitOrigin.x = bounds.center.x;
            exitOrigin.z = bounds.center.z;
            triggerExtent =
                Mathf.Abs(outward.x) * bounds.extents.x +
                Mathf.Abs(outward.z) * bounds.extents.z;
        }

        float playerRadius = 0.5f;
        CapsuleCollider capsule = _player != null ? _player.GetComponent<CapsuleCollider>() : null;
        if (capsule != null)
        {
            Vector3 scale = capsule.transform.lossyScale;
            playerRadius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        }

        float exitDistance = Mathf.Max(
            minimumExitDistance,
            triggerExtent + playerRadius + exitClearance);

        return exitOrigin + outward * exitDistance;
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

        if (useReplacementDoorAnimation)
        {
            yield return AnimateReplacementDoorsOpen();
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

    private IEnumerator AnimateReplacementDoorsOpen()
    {
        float duration = Mathf.Max(0.01f, replacementDoorOpenDuration);
        float openDistance = Mathf.Max(0.001f, replacementDoorOpenDistance);
        Vector3 leftOpenPosition = leftDoorInitialPosition + Vector3.left * openDistance;
        Vector3 rightOpenPosition = rightDoorInitialPosition + Vector3.right * openDistance;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            leftDoor.localPosition = Vector3.LerpUnclamped(
                leftDoorInitialPosition,
                leftOpenPosition,
                progress);
            rightDoor.localPosition = Vector3.LerpUnclamped(
                rightDoorInitialPosition,
                rightOpenPosition,
                progress);
            yield return null;
        }

        leftDoor.localPosition = leftOpenPosition;
        rightDoor.localPosition = rightOpenPosition;
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
        leftDoor = FindDescendant(animationRoot, "elevator_doors_left");
        rightDoor = FindDescendant(animationRoot, "elevator_doors_right");
        useReplacementDoorAnimation = leftDoor != null && rightDoor != null;

        if (!useReplacementDoorAnimation)
        {
            leftDoor = FindDescendant(animationRoot, "left_door.003");
            rightDoor = FindDescendant(animationRoot, "right_door.003");
        }

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

    private static Transform FindDescendant(Transform root, string objectName)
    {
        foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
        {
            if (candidate.name == objectName)
            {
                return candidate;
            }
        }

        return null;
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
