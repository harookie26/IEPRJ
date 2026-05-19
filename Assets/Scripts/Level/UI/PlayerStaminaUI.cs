using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerStaminaUI : MonoBehaviour
{
    [Header("Player References")]
    [Tooltip("Reference to the player movement component that owns stamina state.")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("UI References")]
    [Tooltip("Root GameObject for the circular stamina HUD. It will be shown only while sprinting or regaining stamina.")]
    [SerializeField] private GameObject staminaHudRoot;

    [Tooltip("Radial Image used to display current stamina. The image will be forced to a filled radial type automatically.")]
    [SerializeField] private Image staminaFillImage;

    [Header("Low Stamina Warning")]
    [Tooltip("Stamina percentage threshold to trigger flickering (0-1).")]
    [SerializeField] private float lowStaminaThreshold = 0.25f;

    [Tooltip("Speed of the flickering effect when stamina is low.")]
    [SerializeField] private float flickerSpeed = 4f;

    [Tooltip("Color to apply when stamina is low (used for flickering).")]
    [SerializeField] private Color lowStaminaColor = Color.red;

    [Header("Exhausted State")]
    [Tooltip("Color to display when player is exhausted (cannot sprint).")]
    [SerializeField] private Color exhaustedColor = Color.gray;

    private Color originalStaminaColor;
    private float flickerTimer = 0f;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<PlayerMovement>();

        if (staminaFillImage != null)
            originalStaminaColor = staminaFillImage.color;
    }

    private void Start()
    {
        if (staminaFillImage != null)
        {
            staminaFillImage.type = Image.Type.Filled;
            staminaFillImage.fillMethod = Image.FillMethod.Horizontal;
            staminaFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            originalStaminaColor = staminaFillImage.color;
        }

        if (staminaHudRoot != null)
            staminaHudRoot.SetActive(false);
    }

    private void Update()
    {
        if (playerMovement == null || staminaHudRoot == null || staminaFillImage == null)
            return;

        bool shouldShow = playerMovement.IsCurrentlySprinting || playerMovement.CurrentStamina < playerMovement.MaxStamina;
        if (staminaHudRoot.activeSelf != shouldShow)
            staminaHudRoot.SetActive(shouldShow);

        float fillAmount = 0f;
        if (playerMovement.MaxStamina > 0f)
            fillAmount = playerMovement.CurrentStamina / playerMovement.MaxStamina;

        staminaFillImage.fillAmount = fillAmount;

        if (playerMovement.IsExhausted)
        {
            flickerTimer = 0f;
            staminaFillImage.color = exhaustedColor;
        }
        else
        {
            bool isLowStamina = fillAmount < lowStaminaThreshold;
            if (isLowStamina)
            {
                flickerTimer += Time.deltaTime * flickerSpeed;
                float flicker = Mathf.Sin(flickerTimer * Mathf.PI) * 0.5f + 0.5f;
                Color displayColor = Color.Lerp(originalStaminaColor, lowStaminaColor, flicker);
                staminaFillImage.color = displayColor;
            }
            else
            {
                flickerTimer = 0f;
                staminaFillImage.color = originalStaminaColor;
            }
        }
    }
}
