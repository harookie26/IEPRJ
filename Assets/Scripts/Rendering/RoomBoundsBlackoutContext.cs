using UnityEngine;

// Global context updated by LevelCameraDefault (or any camera controller) so the
// URP Renderer Feature can access current room bounds each frame.
public static class RoomBoundsBlackoutContext
{
    public static bool Enabled;
    public static Vector3 RoomMin;
    public static Vector3 RoomMax;
    public static float SoftMargin;
}
