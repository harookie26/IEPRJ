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
    [SerializeField] private GameObject enemy;
    [SerializeField] private GameObject objective;

    [Header("Haunting Settings")]
    [SerializeField] private float hauntingDistance = 2f;
    [SerializeField] private float hiddenHauntingDistance = 2f;
    [SerializeField] private float hauntingTick = 1f;
    [SerializeField] private float maxSanity = 10f;

    [SerializeField] private Volume volume;
    private Vignette vignette;

    private bool isLost;
    private HauntingSystem hauntingSystem;

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
        hauntingSystem.SetHidingSpot(FindCurrentHidingSpot());
    }

    private void OnPlayerRevealed()
    {
        hauntingSystem.ClearHidingSpot();
    }

    void Start()
    {
        isLost = false;

        if (volume != null && volume.profile != null)
        {
            volume.profile.TryGet(out vignette);
        }

        hauntingSystem = new HauntingSystem(
            player,
            enemy,
            hauntingDistance,
            hiddenHauntingDistance,
            hauntingTick,
            maxSanity
        );
    }

    void Update()
    {
        if (isLost)
            return;

        float defeatDistance = 1.0f;

        if (Vector3.Distance(enemy.transform.position, player.transform.position) < defeatDistance)
            HandleDefeat();

        if (Vector3.Distance(objective.transform.position, player.transform.position) < defeatDistance)
            HandleDefeat();

        bool haunted = hauntingSystem.IsHaunted();
        bool reachedMax = hauntingSystem.UpdateSanity();

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

    private void HandleDefeat()
    {
        if (!isLost)
        {
            Debug.Log("Player has been defeated!");
            isLost = true;
            EventBroadcaster.Instance.PostEvent(GameStateEvents.ON_LEVEL_FAILED);
        }
    }

    private void UpdateVignetteEffect()
    {
        if (vignette != null && hauntingSystem != null)
        {
            float t = Mathf.Clamp01(hauntingSystem.CurrentSanity / hauntingSystem.MaxSanity);
            vignette.intensity.value = Mathf.Lerp(0f, 0.5f, t);
        }
    }
    private void OnDrawGizmos()
    {
        if (player != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(player.transform.position, hauntingDistance);
        }

        if (hauntingSystem != null)
        {
            var hidingSpot = typeof(HauntingSystem)
                .GetField("currentHidingSpot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(hauntingSystem) as MonoBehaviour;

            if (hidingSpot != null)
            {
                Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
                Gizmos.DrawWireSphere(hidingSpot.transform.position, hiddenHauntingDistance);
            }
        }

        if (enemy != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f); // Red, more transparent
            Gizmos.DrawWireSphere(enemy.transform.position, hauntingDistance);
        }
    }
}