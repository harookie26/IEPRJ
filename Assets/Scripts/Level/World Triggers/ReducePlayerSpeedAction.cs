using UnityEngine;

public class ReducePlayerSpeedAction : SpatialTriggerAction
{
    [SerializeField] private float slowMultiplier = 0.5f;

    public override void OnEnter(GameObject target)
    {
        PlayerMovement movement = target.GetComponent<PlayerMovement>();

        if (movement == null)
            return;

        movement.SetTriggerSlow(slowMultiplier);
    }

    public override void OnExit(GameObject target)
    {
        PlayerMovement movement = target.GetComponent<PlayerMovement>();

        if (movement == null)
            return;

        movement.ResetTriggerSlow();
    }
}