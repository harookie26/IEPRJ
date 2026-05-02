using System;
using System.Collections.Generic;
using UnityEngine;
using Level.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject interactHUD;
    [SerializeField] private GameObject stairUpHUD;
    [SerializeField] private GameObject stairDownHUD;
    [SerializeField] private GameObject channelHUD;
    [SerializeField] private GameObject respawnHUD;
    [SerializeField] private GameObject collectibleHUD;

    public TMP_Text collectibleText;

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
        public const string Respawn = "respawn";
        public const string Collectible = "collectible";
    }

    private Dictionary<string, GameObject> hudMap;

    private PlayerStateMachine playerStateMachine;

    private string pendingHudKey;
    private string forceHudKey; // HUD shown regardless of idle state

    // Track collectible HUD coroutine so repeated collections reset the timer
    private Coroutine collectibleHudCoroutine;

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
            { Keys.Channel, channelHUD },
            { Keys.Respawn, respawnHUD },
            { Keys.Collectible, collectibleHUD }
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
            if (kv.Value == null) continue;

            // If a HUD is being forced/shown (forceHudKey), don't hide it here.
            if (!string.IsNullOrEmpty(forceHudKey) &&
                string.Equals(kv.Key, forceHudKey, System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            kv.Value.SetActive(false);
        }

        // Also ensure respawn HUD is hidden when hiding all (unless forced)
        if (respawnHUD != null && (string.IsNullOrEmpty(forceHudKey) || !string.Equals(forceHudKey, Keys.Respawn, System.StringComparison.OrdinalIgnoreCase)))
            respawnHUD.SetActive(false);
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

    // Public helper to show the collectible HUD with text and auto-hide after a duration
    public void ShowCollectibleHUD(string collectibleId, float displaySeconds = 3f)
    {
        if (string.IsNullOrEmpty(collectibleId)) return;

        // Stop any previous collectible HUD hide coroutine so repeated collects reset timer
        if (collectibleHudCoroutine != null)
        {
            StopCoroutine(collectibleHudCoroutine);
            collectibleHudCoroutine = null;
        }

        // Mark collectible as forced so other hide calls don't immediately hide it
        forceHudKey = Keys.Collectible;

        // Update text
        if (collectibleText != null)
        {
            collectibleText.text = collectibleId + "!";
        }

        // Activate HUD
        if (collectibleHUD != null)
        {
            try
            {
                if (collectibleHUD.transform.parent != null)
                    collectibleHUD.transform.SetAsLastSibling();
            }
            catch { }

            collectibleHUD.SetActive(true);
            AnimateHUD(collectibleHUD);
        }

        // Start auto-hide coroutine
        collectibleHudCoroutine = StartCoroutine(HideCollectibleHUDAfter(displaySeconds));
    }

    private System.Collections.IEnumerator HideCollectibleHUDAfter(float seconds)
    {
        if (seconds <= 0f) seconds = 0.1f;
        yield return new WaitForSeconds(seconds);

        // Clear reference before hiding
        collectibleHudCoroutine = null;
        // Clear forced key so HideAllHUDs will hide collectible HUD
        if (string.Equals(forceHudKey, Keys.Collectible, System.StringComparison.OrdinalIgnoreCase))
            forceHudKey = null;

        HideAllHUDs();
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