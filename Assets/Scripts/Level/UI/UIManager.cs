using System.Collections.Generic;
using UnityEngine;
using Level.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject interactHUD;
    [SerializeField] private GameObject stairUpHUD;
    [SerializeField] private GameObject stairDownHUD;
    [SerializeField] private GameObject channelHUD;

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

    private void Awake()
    {
        InitializeHudMap();
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

        if (playerStateMachine != null && playerStateMachine.IsIdle)
        {
            ShowHUDImmediate(key);
            pendingHudKey = null;
            return;
        }

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