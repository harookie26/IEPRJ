using Game.ObjectTypes;
using UnityEngine;
using static EventNames;

public class ChannelObject : MonoBehaviour, IChannelable
{
    private float raiseSpeed = 0.5f;
    private float progressMax = 10f;
    private float graceDistance = 0.4f;
    private float progress = 0f;
    private bool isChanneling = false;
    private bool isComplete = false;
    private GameObject currentPlayer;

    public bool IsChanneling => isChanneling;
    public float Progress => progress;
    public float ProgressMax => progressMax;
    public bool IsComplete => isComplete;

    private void Update()
    {
        if (isComplete) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        bool playerNearby = CanChannel(player);

        if (playerNearby && !isChanneling && InputManager.Instance.WasChannelPressed())
        {
            StartChannel(player);
        }
        else if (isChanneling)
        {
            if (InputManager.Instance.WasAnyKeyExceptChannelPressed())
            {
                StopChannel();
            }
            else
            {
                ChannelTick(Time.deltaTime);
                if (isComplete)
                {
                    StopChannel();
                    Destroy(gameObject);
                }
            }
        }
    }

    public bool CanChannel(GameObject player)
    {
        if (isComplete) return false;
        if (player == null) return false;
        return Mathf.Abs(player.transform.position.x - transform.position.x) <= graceDistance;
    }

    public void StartChannel(GameObject player)
    {
        if (!CanChannel(player)) return;
        isChanneling = true;
        currentPlayer = player;
        EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_CHANNELING);
        EventBroadcaster.Instance.PostEvent(ControlEvents2D.ON_2D_PLAYERMOVEMENT_DISABLED);
    }

    public void StopChannel()
    {
        if (!isChanneling) return;
        isChanneling = false;
        currentPlayer = null;
        EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_DECHANNELING);
        EventBroadcaster.Instance.PostEvent(ControlEvents2D.ON_2D_PLAYERMOVEMENT_ENABLED);
    }

    public void ChannelTick(float deltaTime)
    {
        if (!isChanneling || isComplete) return;

        if (TryGetComponent<Rigidbody2D>(out var rb) && rb != null)
        {
            Vector2 targetPosition = rb.position + Vector2.up * raiseSpeed * deltaTime;
            rb.MovePosition(targetPosition);
        }
        else
        {
            transform.position += Vector3.up * raiseSpeed * deltaTime;
        }

        progress += deltaTime;
        if (progress >= progressMax)
        {
            progress = progressMax;
            isComplete = true;
        }
    }
}