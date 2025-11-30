using System;
using System.Collections.Generic;
using UnityEngine;
using Level.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject interactHUD;
    [SerializeField] private GameObject stairUpHUD;
    [SerializeField] private GameObject stairDownHUD;
    [SerializeField] private GameObject channelHUD;
    [SerializeField] private GameObject respawnHUD;

    [Header("Fade Transition Settings")]
    [SerializeField] private GameObject fadePanel;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float blackScreenHoldTime = 1f;


    public static class Keys
    {
        public const string Interact = "interact";
        public const string StairUp = "stair_up";
        public const string StairDown = "stair_down";
        public const string Channel = "channel";
    }

    private Dictionary<string, GameObject> hudMap;

    private PlayerStateMachine playerStateMachine;

    private string pendingHudKey;
    private string forceHudKey; // HUD shown regardless of idle state

    private void Awake()
    {
        InitializeHudMap();

        // Ensure the fade panel is rendered behind other UI siblings on the same Canvas
        if (fadePanel != null && fadePanel.transform.parent != null)
        {
            try
            {
                fadePanel.transform.SetAsFirstSibling();
            }
            catch { }
        }

        // Ensure respawn HUD starts hidden and is on top so it won't be covered by the fade panel
        if (respawnHUD != null && respawnHUD.transform.parent != null)
        {
            try
            {
                respawnHUD.SetActive(false);
                respawnHUD.transform.SetAsLastSibling();
            }
            catch { }
        }
    }

    private void Start()
    {
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
        if (!string.IsNullOrEmpty(pendingHudKey))
        {
            ShowHUDImmediate(pendingHudKey);
            pendingHudKey = null;
        }
    }

    private void OnPlayerIdleExited()
    {
        pendingHudKey = null;
        if (string.IsNullOrEmpty(forceHudKey))
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

    public void ShowHUD(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            pendingHudKey = null;
            HideAllHUDs();
            return;
        }

        pendingHudKey = key;

        // Interact HUD should display immediately regardless of idle state
        if (string.Equals(key, Keys.Interact, StringComparison.OrdinalIgnoreCase))
        {
            ShowHUDImmediate(key);
            pendingHudKey = null;
            return;
        }

        if (playerStateMachine != null && playerStateMachine.IsIdle)
        {
            ShowHUDImmediate(key);
            pendingHudKey = null;
            return;
        }

        HideAllHUDs();
    }

    // Force show a HUD immediately ignoring idle state. Used for door stair indicators.
    public void ShowHUDForce(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        forceHudKey = key;
        ShowHUDImmediate(key);
    }

    public void ClearForcedHUD()
    {
        forceHudKey = null;
        HideAllHUDs();
    }

    public void ShowHUD(GameObject hud)
    {
        if (hud == null)
        {
            pendingHudKey = null;
            HideAllHUDs();
            return;
        }

        // If this is the interact HUD, allow it to show immediately regardless of idle state
        if (hud == interactHUD)
        {
            HideAllHUDs();
            hud.SetActive(true);
            AnimateHUD(hud);
            return;
        }

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
        forceHudKey = null;
        HideAllHUDs();
    }

    private void HideAllHUDs()
    {
        if (hudMap == null) InitializeHudMap();

        foreach (var kv in hudMap)
        {
            if (kv.Value != null) kv.Value.SetActive(false);
        }

        // Also ensure respawn HUD is hidden when hiding all
        if (respawnHUD != null) respawnHUD.SetActive(false);
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

    // New public helpers for respawn HUD control used by checkpoint flow
    public void ShowRespawnHUD()
    {
        if (respawnHUD == null) return;
        // Ensure respawn HUD is top sibling so it's not covered
        try
        {
            if (respawnHUD.transform.parent != null)
                respawnHUD.transform.SetAsLastSibling();
        }
        catch { }

        respawnHUD.SetActive(true);
        // Optionally animate text if present
        AnimateHUD(respawnHUD);
    }

    public void HideRespawnHUD()
    {
        if (respawnHUD == null) return;
        respawnHUD.SetActive(false);
    }

    // Hide a specific HUD by key
    public void HideHUD(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (hudMap == null) InitializeHudMap();

        if (hudMap.TryGetValue(key, out var hud) && hud != null)
        {
            hud.SetActive(false);
        }
    }
}