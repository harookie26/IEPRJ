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

        float staminaRatio = Mathf.Clamp01(currentStamina / maxStamina);
        int activeBars = Mathf.FloorToInt(staminaRatio * totalBars);


        for (int i = 0; i < totalBars; i++)
        {
            staminaBars[i].enabled = i < activeBars;
        }

        Debug.Log($"Stamina: {currentStamina} / {maxStamina} => Bars: {activeBars}");

    }

}