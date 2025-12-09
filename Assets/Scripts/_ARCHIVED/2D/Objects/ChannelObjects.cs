//using Game.ObjectTypes;
//using UnityEngine;
//using static EventNames;

//public class ChannelObject : MonoBehaviour, IChannelable
//{
//    private float raiseSpeed = 0.5f;
//    private float progressMax = 5;
//    private float graceDistance = 0.4f;
//    private float progress = 0f;
//    private bool isChanneling = false;
//    private bool isComplete = false;
//    private GameObject currentPlayer;

//    public bool IsChanneling => isChanneling;
//    public float Progress => progress;
//    public float ProgressMax => progressMax;
//    public bool IsComplete => isComplete;

//    private void Update()
//    {
//        if (isComplete) return;

//        GameObject _player = GameObject.FindGameObjectWithTag("Player");
//        if (_player == null) return;

//        bool playerNearby = CanChannel(_player);

//        if (playerNearby && !isChanneling)
//        {
//            StartChannel(_player);
//        }
//        else if (isChanneling)
//        {
//            if (InputManager.Instance.WasAnyKeyExceptChannelPressed())
//            {
//                StopChannel();
//            }
//            else
//            {
//                ChannelTick(Time.deltaTime);
//                if (isComplete)
//                {
//                    StopChannel();
//                    Destroy(gameObject);
//                }
//            }
//        }
//    }

//    public bool CanChannel(GameObject _player)
//    {
//        if (isComplete) return false;
//        if (_player == null) return false;
//        return Mathf.Abs(_player.transform.position.x - transform.position.x) <= graceDistance;
//    }

//    public void StartChannel(GameObject _player)
//    {
//        if (!CanChannel(_player)) return;
//        isChanneling = true;
//        currentPlayer = _player;
//    }

//    public void StopChannel()
//    {
//        if (!isChanneling) return;
//        isChanneling = false;
//        currentPlayer = null;
//    }

//    public void ChannelTick(float deltaTime)
//    {
//        if (!isChanneling || isComplete) return;

//        if (TryGetComponent<Rigidbody2D>(out var _rb) && _rb != null)
//        {
//            Vector2 targetPosition = _rb.position + Vector2.up * raiseSpeed * deltaTime;
//            _rb.MovePosition(targetPosition);
//        }
//        else
//        {
//            transform.position += Vector3.up * raiseSpeed * deltaTime;
//        }

//        progress += deltaTime;
//        if (progress >= progressMax)
//        {
//            progress = progressMax;
//            isComplete = true;
//        }
//    }
//}