using System.Collections;
using UnityEngine;

public class AutoWalkController : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;

    [SerializeField] private Transform[] waypoints;

    [SerializeField] private float stopDistance = 0.15f;
    [SerializeField] private float rotationSpeed = 5f;

    public void PlayCutscene()
    {
        StartCoroutine(CutsceneRoutine());
    }

    private IEnumerator CutsceneRoutine()
    {
        player.SetCanMove(true);

        foreach (Transform point in waypoints)
        {
            yield return MovePlayerTo(point.position);
        }

        player.ClearExternalMovement();
    }

    private IEnumerator MovePlayerTo(Vector3 targetPos)
    {
        while (true)
        {
            Vector3 toTarget = targetPos - player.transform.position;

            toTarget.y = 0f;

            float distance = toTarget.magnitude;

            if (distance <= stopDistance)
                break;

            Vector3 dir = toTarget.normalized;

            // Rotate player naturally
            player.RotateTowards(dir, rotationSpeed);

            // Convert world movement to local input
            Vector3 localDir =
                player.transform.InverseTransformDirection(dir);

            Vector2 moveInput =
                new Vector2(localDir.x, localDir.z);

            player.SetExternalMovement(moveInput, Vector2.zero);

            yield return null;
        }

        player.SetExternalMovement(Vector2.zero, Vector2.zero);
    }
}