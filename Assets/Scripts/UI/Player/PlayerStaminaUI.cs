using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStaminaUI : MonoBehaviour
{
    [Header("Stamina Bar Images (Order matters: left-to-right or bottom-to-top)")]
    public List<Image> staminaBars;

    [Header("Reference to PlayerMovement2D")]
    public PlayerMovement2D playerMovement;

    private void Update()
    {
        if (playerMovement == null || staminaBars == null || staminaBars.Count == 0)
            return;

        float maxStamina = playerMovement.maxStamina;
        float currentStamina = playerMovement.GetCurrentStamina();
        int totalBars = staminaBars.Count;

        float staminaPercent = Mathf.Clamp01(currentStamina / maxStamina);

        for (int i = 0; i < totalBars; i++)
        {
            float barThreshold = (i + 1) / (float)totalBars;
            staminaBars[i].enabled = staminaPercent >= barThreshold && staminaPercent > 0f;
        }
    }
}