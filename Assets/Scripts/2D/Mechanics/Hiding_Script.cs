using UnityEngine;
using System.Collections.Generic;
using static EventNames;
using System.Collections;

public class Hiding_Script : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private float graceDistance = 0.2f;
    [SerializeField] private List<GameObject> hidingSpotPrefabs;
    [SerializeField] private List<Vector3> hidingSpotPositions;
    [SerializeField] private float hidingDuration;
    [SerializeField] private float hidingSpotCooldown = 3f; // Cooldown in seconds

    private float timer;
    private GameObject targetObject;
    private int targetIndex = -1;
    private SpriteRenderer playerSpriteRenderer;
    private readonly List<GameObject> hidingSpots = new();
    private bool canHide;
    private PlayerHeatMap playerHeatMap;

    public Transform hidingPosition;

    public bool isHiding { get; private set; }

    // Cooldown tracker for each hiding spot
    private Dictionary<GameObject, float> hidingSpotCooldowns = new();

    private void Start()
    {
        int count = Mathf.Min(hidingSpotPrefabs.Count, hidingSpotPositions.Count);
        for (int i = 0; i < count; i++)
        {
            GameObject spot = Instantiate(hidingSpotPrefabs[i], hidingSpotPositions[i], Quaternion.identity);
            hidingSpots.Add(spot);
            hidingSpotCooldowns[spot] = 0f; // Initialize cooldown

            if (hidingSpotPrefabs[i] != null && hidingSpotPrefabs[i].scene.IsValid())
            {
                Destroy(hidingSpotPrefabs[i]);
            }
        }

        playerSpriteRenderer = player.GetComponent<SpriteRenderer>();
        playerHeatMap = player.GetComponentInChildren<PlayerHeatMap>();
        canHide = false;
        isHiding = false;
    }

    private void Update()
    {
        UpdateCooldowns();
        CheckHidingSpot();

        if (Input.GetKeyDown(KeyCode.W))
        {
            HandleHidingInput();
        }

        if (isHiding)
        {
            HidePlayer();
        }
        else
        {
            RevealPlayer();
        }

        if (targetObject != null && isHiding)
        {
            timer += Time.deltaTime;
            if (timer >= hidingDuration)
            {
                RemoveHidingSpot();
            }
        }
    }

    // Decrement cooldown timers
    private void UpdateCooldowns()
    {
        var keys = new List<GameObject>(hidingSpotCooldowns.Keys);
        foreach (var spot in keys)
        {
            if (hidingSpotCooldowns[spot] > 0f)
                hidingSpotCooldowns[spot] -= Time.deltaTime;
        }
    }

    private void CheckHidingSpot()
    {
        canHide = false;
        targetObject = null;
        targetIndex = -1;

        for (int i = 0; i < hidingSpots.Count; i++)
        {
            if (Mathf.Abs(hidingSpots[i].transform.position.x - player.transform.position.x) <= graceDistance)
            {
                // Only allow hiding if cooldown is finished
                if (hidingSpotCooldowns[hidingSpots[i]] <= 0f)
                {
                    targetObject = hidingSpots[i];
                    targetIndex = i;
                    canHide = true;
                }
                break;
            }
        }
    }

    private void HandleHidingInput()
    {
        if (canHide && !isHiding)
        {
            isHiding = true;
            EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_HID);
        }
        else if (isHiding)
        {
            isHiding = false;
            timer = 0f;
            // Start cooldown for the spot just exited
            if (targetObject != null)
                hidingSpotCooldowns[targetObject] = hidingSpotCooldown;
            targetObject = null;
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            EventBroadcaster.Instance.PostEvent(ObjectEvents.OBJECT_HIDING_COOLDOWN);
            EventBroadcaster.Instance.PostEvent(PlayerEvents.PLAYER_REVEALED);
        }
        else
        {
            Debug.Log("No hiding spot available");
        }
    }

    private void HidePlayer()
    {
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        var movement = player.GetComponent<PlayerMovement2D>();
        if (movement != null)
            movement.enabled = false;

        if (playerSpriteRenderer != null)
            playerSpriteRenderer.enabled = false;

        if (playerHeatMap != null)
        { 
            playerHeatMap.SetHeatMapVisible(true);
            playerHeatMap.StartGrowing();
        }

        if (targetObject != null)
        {
            hidingPosition = targetObject.transform;
        }
        else
        {
            Debug.LogWarning("Target object is null, cannot hide player.");
        }
    }

    private void RevealPlayer()
    {

        var movement = player.GetComponent<PlayerMovement2D>();
        if (movement != null)
        {
            movement.enabled = true;
        }

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (playerSpriteRenderer != null)
            playerSpriteRenderer.enabled = true;

        if (playerHeatMap != null)
        {
            playerHeatMap.SetHeatMapVisible(false);
            playerHeatMap.StopGrowingAndReset();
        }

        hidingPosition = null;
    }
    private void RemoveHidingSpot()
    {
        if (targetIndex >= 0 && targetIndex < hidingSpotPositions.Count && targetIndex < hidingSpotPrefabs.Count)
        {
            hidingSpotPositions.RemoveAt(targetIndex);
            hidingSpotPrefabs.RemoveAt(targetIndex);
        }
        targetObject = null;
        targetIndex = -1;
        timer = 0f;
    }
}
