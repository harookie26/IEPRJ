using UnityEngine;

[System.Serializable]
public class PlayerHidingSettings
{
    [Header("Movement")]
    public float wallMoveSpeed = 3f;
    public float rotationSpeed = 10f;

    [Header("Snap / Hug")]
    public float hugOffset = 0.5f;
    public float snapDuration = 0.25f;

    [Header("Exit Nudge")]
    public float nudgeDistance = 0.5f;
    public float nudgeDuration = 0.2f;

    [Header("Corner Detection")]
    public bool enableCornerDetection = true;
    public float cornerLookAhead = 0.25f;
    public float cornerProbeHeight = 0.5f;
    [Range(0f, 1f)] public float cornerNormalDotThreshold = 0.75f;
    public float cornerFailSlack = 0.3f;
    public bool debugCorner = false;
}