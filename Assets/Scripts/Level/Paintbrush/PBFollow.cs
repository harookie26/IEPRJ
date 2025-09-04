using UnityEngine;

public class PBFollow : MonoBehaviour
{
    [SerializeField] private Vector3 offset;
    [SerializeField] private float followSpeed = 5f;

    private Transform player;
    private Vector3 lastPlayerPosition;
    private int side = -1;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            lastPlayerPosition = player.position;
        }
        else
            Debug.LogError("Player object with tag 'Player' not found in the scene.");
    }

    private void Update()
    {
        if (player == null) return;

        float deltaX = player.position.x - lastPlayerPosition.x;
        if (Mathf.Abs(deltaX) > 0.01f)
        {
            int newSide = deltaX > 0 ? -1 : 1;
            
            if (newSide != side)
            {
                side = newSide;
                offset.x = Mathf.Abs(offset.x) * side;
            }
        }

        Vector3 targetPosition = player.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

        lastPlayerPosition = player.position;
    }
}