using Game.ObjectTypes;
using UnityEngine;
using static EventNames;

public class HidableObject : MonoBehaviour, IHidable
{
    [SerializeField] private float cooldown = 10f;
    [SerializeField] private float graceDistance = 0.5f;
    [SerializeField] private float hidingDuration = 20;

    private float cooldownTimer = 0f;
    private bool isHiding = false;
    private float hidingTimer = 0f;
    private SpriteRenderer sr;
    private GameObject currentPlayer;

    public bool IsAvailable => cooldownTimer <= 0f && !isHiding;
    public bool IsPlayerHiding => isHiding;
    public float CooldownRemaining => Mathf.Max(0f, cooldownTimer);

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            SetAlpha(0.4f);
        }
        else
        {
            SetAlpha(1f);
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        bool playerNearby = player != null && Mathf.Abs(transform.position.x - player.transform.position.x) <= graceDistance;

        if (isHiding)
        {
            hidingTimer += Time.deltaTime;
            if (hidingTimer >= hidingDuration)
            {
                EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_REVEALED);
                ExitHiding();
            }
            // Allow exit with W
            if (InputManager.Instance.WasInteractPressed())
            {
                EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_REVEALED);
                ExitHiding();
            }
        }
        else if (playerNearby && cooldownTimer <= 0f)
        {
            // Allow enter with W
            if (InputManager.Instance.WasInteractPressed())
            {
                EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_HID);
                EnterHiding(player);
            }
        }
    }

    private void SetAlpha(float alpha)
    {
        if (sr != null)
        {
            var color = sr.color;
            color.a = alpha;
            sr.color = color;
        }
    }

    //private void OnGUI()
    //{
    //    GameObject _player = GameObject.FindGameObjectWithTag("Player");
    //    bool playerNearby = _player != null && Mathf.Abs(transform.position.x - _player.transform.position.x) <= graceDistance;

    //    if (playerNearby && !isHiding && cooldownTimer <= 0f)
    //    {
    //        GUI.Label(new Rect(Screen.width / 2, Screen.height / 2, 200, 30), "Press W to hide");
    //    }
    //    else if (isHiding)
    //    {
    //        GUI.Label(new Rect(Screen.width / 2, Screen.height / 2, 200, 30), "Press W to exit hiding");
    //    }
    //}

    public void EnterHiding(GameObject player)
    {
        if (!IsAvailable || player == null) return;

        isHiding = true;
        hidingTimer = 0f;
        currentPlayer = player;

        var srPlayer = player.GetComponent<SpriteRenderer>();
        if (srPlayer != null) srPlayer.enabled = false;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    public void ExitHiding()
    {
        if (!isHiding) return;

        isHiding = false;
        cooldownTimer = cooldown;
        hidingTimer = 0f;

        if (currentPlayer != null)
        {
            var srPlayer = currentPlayer.GetComponent<SpriteRenderer>();
            if (srPlayer != null) srPlayer.enabled = true;

            currentPlayer = null;
        }
    }
}