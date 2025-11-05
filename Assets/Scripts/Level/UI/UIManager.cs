using System.Collections.Generic;
using UnityEngine;
using Level.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject interactHUD;
    [SerializeField] private GameObject stairUpHUD;
    [SerializeField] private GameObject stairDownHUD;
    [SerializeField] private GameObject channelHUD;

    public static class Keys
    {
        public const string Interact = "interact";
        public const string StairUp = "stair_up";
        public const string StairDown = "stair_down";
        public const string Channel = "channel";
    }

    private Dictionary<string, GameObject> hudMap;

    // player reference to observe idle state
    private PlayerStateMachine playerStateMachine;

    // last requested HUD key (kept when player is not idle)
    private string pendingHudKey;

    private void Awake()
    {
        InitializeHudMap();
    }

    private void Start()
    {
        // subscribe to player idle events; Start runs after typical Awake initializations
        playerStateMachine = FindFirstObjectByType<PlayerStateMachine>();
        if (playerStateMachine != null)
        {
            playerStateMachine.IdleEntered += OnPlayerIdleEntered;
            playerStateMachine.IdleExited += OnPlayerIdleExited;
        }
    }

    private void OnDestroy()
    {
        if (playerStateMachine != null)
        {
            playerStateMachine.IdleEntered -= OnPlayerIdleEntered;
            playerStateMachine.IdleExited -= OnPlayerIdleExited;
        }
    }

    private void OnPlayerIdleEntered()
    {
        // when idle starts, show pending HUD if any
        if (!string.IsNullOrEmpty(pendingHudKey))
        {
            ShowHUDImmediate(pendingHudKey);
            pendingHudKey = null;
        }
    }

    private void OnPlayerIdleExited()
    {
        // hide immediately when leaving idle
        pendingHudKey = null;
        HideAllHUDs();
    }

    private void InitializeHudMap()
    {
        hudMap = new Dictionary<string, GameObject>(System.StringComparer.OrdinalIgnoreCase)
        {
            { Keys.Interact, interactHUD },
            { Keys.StairUp, stairUpHUD },
            { Keys.StairDown, stairDownHUD },
            { Keys.Channel, channelHUD }
        };
    }

    // Called by callers (PlayerInteractor) — caller remains responsible for detecting what HUD is relevant.
    // UIManager now owns idle gating and will show immediately only when player.IsIdle; otherwise it defers.
    public void ShowHUD(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            pendingHudKey = null;
            HideAllHUDs();
            return;
        }

        // remember the requested HUD
        pendingHudKey = key;

        // if player is available and currently idle, show immediately
        if (playerStateMachine != null && playerStateMachine.IsIdle)
        {
            ShowHUDImmediate(key);
            pendingHudKey = null;
            return;
        }

        // not idle -> keep pending and ensure HUDs are hidden for now
        HideAllHUDs();
    }

    // Overload kept for callers that pass GameObject directly. Will only show when idle.
    public void ShowHUD(GameObject hud)
    {
        if (hud == null)
        {
            pendingHudKey = null;
            HideAllHUDs();
            return;
        }

        // If we can map this GameObject to a key, set pendingHudKey accordingly (optional).
        // Simpler: only show immediate if player is idle; otherwise hide.
        if (playerStateMachine != null && playerStateMachine.IsIdle)
        {
            HideAllHUDs();
            hud.SetActive(true);
            AnimateHUD(hud);
        }
        else
        {
            HideAllHUDs();
        }
    }

    // Internal helper that performs the actual show without re-checking idle
    private void ShowHUDImmediate(string key)
    {
        if (hudMap == null) InitializeHudMap();

        if (hudMap.TryGetValue(key, out var hud) && hud != null)
        {
            HideAllHUDs();
            hud.SetActive(true);
            AnimateHUD(hud);
        }
        else
        {
            Debug.LogWarning($"UIManager.ShowHUDImmediate: HUD not found or not assigned for key '{key}'");
        }
    }

    public void RegisterHUD(string key, GameObject hud)
    {
        if (string.IsNullOrEmpty(key) || hud == null) return;
        if (hudMap == null) InitializeHudMap();
        hudMap[key] = hud;
    }

    public void HideAll()
    {
        pendingHudKey = null;
        HideAllHUDs();
    }

    private void HideAllHUDs()
    {
        if (hudMap == null) InitializeHudMap();

        foreach (var kv in hudMap)
        {
            if (kv.Value != null) kv.Value.SetActive(false);
        }
    }

    private void AnimateHUD(GameObject hud)
    {
        if (hud == null) return;

        var animators = hud.GetComponentsInChildren<TMPTextAnimator>(true);
        foreach (var animator in animators)
        {
            if (animator == null) continue;

            var tmp = animator.GetComponent<UnityEngine.UI.Text>() as UnityEngine.UI.Text;
            var tmpPro = animator.GetComponent<TMPro.TextMeshProUGUI>();

            string message = null;
            if (tmpPro != null) message = tmpPro.text;
            else if (tmp != null) message = tmp.text;

            if (string.IsNullOrEmpty(message)) continue;

            animator.PlayFadeInWhole(message, 0.25f);
        }
    }
}