using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    /// <summary>
    /// The _player's transform to follow.
    /// </summary>
    public Transform player;

    /// <summary>
    /// The speed at which the camera follows the _player.
    /// </summary>
    public float smoothSpeed = 0.125f;

    /// <summary>
    /// The offset from the _player.
    /// </summary>
    public Vector3 offset;

    /// <summary>
    /// LateUpdate is called after all Update functions have been called. 
    /// This is the recommended place to put camera follow logic.
    /// </summary>
    void LateUpdate()
    {
        if (player != null)
        {
            Vector3 desiredPosition = player.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
        }
    }
}
