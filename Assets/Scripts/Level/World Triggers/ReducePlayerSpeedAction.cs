using UnityEngine;

public class ReducePlayerSpeedAction : SpatialTriggerAction
{
    [SerializeField] private Transform lookTarget;
    [SerializeField] private float lookAtSpeed;

    PlayerCamera playerCamera;
    PlayerMovement playerMovement;

    private void Start()
    {
        playerCamera = FindFirstObjectByType<PlayerCamera>();
        playerMovement = FindFirstObjectByType<PlayerMovement>();
    }

    public override void OnEnter(GameObject target)
    {
        playerCamera.SetLookAtTarget(lookTarget);
        playerMovement.RotateTowards(lookTarget.position, lookAtSpeed);
    }

    public override void OnExit(GameObject target)
    {
        playerCamera.ClearLookAtTarget();
    }
}