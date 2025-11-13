using UnityEngine;
using static EventNames;

public class CameraMotion : MonoBehaviour
{
    private bool isWalking = false, isRunning = false, isStopped = false;

    // Store the original camera position to reset when stopped
    private Vector3 originalPosition;

    // Parameters for motion effect
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float walkBobSpeed = 6f;
    [SerializeField] private float runBobAmount = 0.09f;
    [SerializeField] private float runBobSpeed = 10f;

    private void Awake()
    {
        EventBroadcaster.Instance.AddObserver(PlayerEvents.PLAYER_MOVED, PlayerWalking);
        EventBroadcaster.Instance.AddObserver(PlayerEvents.PLAYER_STARTED_SPRINT, PlayerRunning);
        EventBroadcaster.Instance.AddObserver(PlayerEvents.PLAYER_STOPPED, PlayerStopped);

        originalPosition = transform.localPosition;
    }

    private void OnDestroy()
    {
        EventBroadcaster.Instance.RemoveActionAtObserver(PlayerEvents.PLAYER_MOVED, PlayerWalking);
        EventBroadcaster.Instance.RemoveActionAtObserver(PlayerEvents.PLAYER_STARTED_SPRINT, PlayerRunning);
        EventBroadcaster.Instance.RemoveActionAtObserver(PlayerEvents.PLAYER_STOPPED, PlayerStopped);
    }

    private void PlayerWalking()
    {
        isWalking = true;
        isRunning = false;
        isStopped = false;
    }

    private void PlayerRunning()
    {
        isWalking = false;
        isRunning = true;
        isStopped = false;
    }

    private void PlayerStopped()
    {
        isWalking = false;
        isRunning = false;
        isStopped = true;
    }

    private void CameraMotionEffect()
    {
        if (isWalking)
        {
            // Slight up and down motion for walking
            float bobOffset = Mathf.Sin(Time.time * walkBobSpeed) * walkBobAmount;
            transform.localPosition = originalPosition + new Vector3(0, bobOffset, 0);
        }
        else if (isRunning)
        {
            // Increased intensity for running
            float bobOffset = Mathf.Sin(Time.time * runBobSpeed) * runBobAmount;
            transform.localPosition = originalPosition + new Vector3(0, bobOffset, 0);
        }
        else if (isStopped)
        {
            // Reset camera position when stopped
            transform.localPosition = originalPosition;
        }
    }

    private void Update()
    {
        CameraMotionEffect();
    }
}
