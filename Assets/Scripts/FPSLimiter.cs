using UnityEngine;

public static class FPSLimiter
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeFPSCap()
    {
        QualitySettings.vSyncCount = 0;

        Application.targetFrameRate = 60;

        Debug.Log("[FPSLimiter] Game frame rate capped to 60 FPS globally.");
    }
}