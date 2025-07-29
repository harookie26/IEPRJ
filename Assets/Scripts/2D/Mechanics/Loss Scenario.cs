using System.Linq;
using Game.ObjectTypes;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static EventNames;

public class LossScenario : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject ghost;

    [Header("Haunting Settings")]
    [SerializeField] private float hauntingDistance = 2f;
    [SerializeField] private float hiddenHauntingDistance = 2f;
    [SerializeField] private float hauntingTick = 1f;
    [SerializeField] private float maxSanity = 10f;

    [SerializeField] private Volume volume;
    private Vignette vignette;

    private float currentSanity;
    private float tickTimer;
    private bool isHaunted;
    private bool isLost;

    private IHidable currentHidingSpot;

    private void OnEnable()
    {
        EventBroadcaster.Instance.AddObserver(PlayerEvents.PLAYER_HID, OnPlayerHiding);
        EventBroadcaster.Instance.AddObserver(PlayerEvents.PLAYER_REVEALED, OnPlayerRevealed);
    }

    private void OnDisable()
    {
        EventBroadcaster.Instance.RemoveActionAtObserver(PlayerEvents.PLAYER_HID, OnPlayerHiding);
        EventBroadcaster.Instance.RemoveActionAtObserver(PlayerEvents.PLAYER_REVEALED, OnPlayerRevealed);
    }

    private void OnPlayerHiding()
    {
        currentHidingSpot = FindCurrentHidingSpot();
    }

    private void OnPlayerRevealed()
    {
        currentHidingSpot = null;
    }

    void Start()
    {
        currentSanity = 0f;
        isLost = false;
        tickTimer = 0f;

        if (volume != null && volume.profile != null)
        {
            volume.profile.TryGet(out vignette);
        }
    }

    void Update()
    {
        if (isLost)
            EventBroadcaster.Instance.PostEvent(GameStateEvents.ON_LEVEL_FAILED);

        bool isHidden = currentHidingSpot != null && currentHidingSpot.IsPlayerHiding;

        if (isHidden)
            isHaunted = IsHidingPlayerHaunted();
        else
            isHaunted = IsPlayerHaunted();

        tickTimer += Time.deltaTime;

        if (isHaunted)
        {
            TryIncreaseSanity();
        }
        else
        {
            TryDecreaseSanity();
        }

        if (currentSanity >= maxSanity)
        {
            HandleDefeat();
        }

        UpdateVignetteEffect();
    }

    private IHidable FindCurrentHidingSpot()
    {
        var hideables = Object.FindObjectsByType<HidableObject>(FindObjectsSortMode.None);
        foreach (var hideable in hideables)
        {
            if (hideable.IsPlayerHiding)
                return hideable;
        }
        return null;
    }

    private bool IsHidingPlayerHaunted()
    {
        if (currentHidingSpot is MonoBehaviour hidingMono)
        {
            float distance = Mathf.Abs(hidingMono.transform.position.x - ghost.transform.position.x);
            return distance <= hiddenHauntingDistance;
        }
        return false;
    }

    private bool IsPlayerHaunted()
    {
        float distance = Mathf.Abs(player.transform.position.x - ghost.transform.position.x);
        return distance <= hauntingDistance;
    }

    private void TryIncreaseSanity()
    {
        if (currentSanity < maxSanity && tickTimer >= hauntingTick)
        {
            currentSanity = Mathf.Clamp(currentSanity + 1, 0, maxSanity);
            tickTimer = 0f;
        }
    }

    private void TryDecreaseSanity()
    {
        if (currentSanity > 0 && tickTimer >= hauntingTick * 2)
        {
            currentSanity = Mathf.Clamp(currentSanity - 1, 0, maxSanity);
            Debug.Log($"Current Haunted Points: {currentSanity}/{maxSanity}");
            tickTimer = 0f;
        }
    }

    private void HandleDefeat()
    {
        isLost = true;
    }

    private void UpdateVignetteEffect()
    {
        if (vignette != null)
        {
            float t = Mathf.Clamp01(currentSanity / maxSanity);
            vignette.intensity.value = Mathf.Lerp(0f, 0.75f, t);
        }
    }
}