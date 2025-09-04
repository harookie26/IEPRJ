using UnityEngine;
using System.Collections;

public class LevelCameraDefault : MonoBehaviour
{
    public Transform player;
    public float smoothSpeed = 0.125f;
    public Vector3 offset;


    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void LateUpdate()
    {
        Vector3 targetPos = new Vector3(player.position.x, player.position.y, 0) + offset;
        Vector3 smoothedPos = Vector3.Lerp(transform.position, targetPos, smoothSpeed);
        transform.position = smoothedPos;

        transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    public Vector3 GetOffset()
    {
        return offset;
    }
}
