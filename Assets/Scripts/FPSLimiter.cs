using UnityEngine;

public static class FPSLimiter
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeFPSCap()
    {
        QualitySettings.vSyncCount = 1;

        Application.targetFrameRate = -1;

        Debug.Log("[FPSLimiter] Game frame rate capped to 60 FPS globally.");
    }
}