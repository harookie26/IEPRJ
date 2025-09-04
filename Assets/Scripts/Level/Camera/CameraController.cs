using UnityEngine;

public class CameraController : MonoBehaviour
{
    public enum Mode { Default, Companion }
    public Mode CurrentMode { get; private set; } = Mode.Default;

    [SerializeField] private LevelCameraDefault defaultCamera;
    [SerializeField] private LevelCameraCompanion companionCamera;

    private void Awake()
    {
        if (defaultCamera == null)
            defaultCamera = GetComponent<LevelCameraDefault>();
        if (companionCamera == null)
            companionCamera = GetComponent<LevelCameraCompanion>();
    }

    private void Start()
    {
        SetMode(CurrentMode);
    }

    private void Update()
    {
        if (PBController.IsCompanionManualModeActive && CurrentMode != Mode.Companion)
            SetMode(Mode.Companion);
        else if (!PBController.IsCompanionManualModeActive && CurrentMode == Mode.Companion)
            SetMode(Mode.Default);
    }

    private void SetMode(Mode mode)
    {
        if (defaultCamera != null)
            defaultCamera.enabled = (mode == Mode.Default);
        if (companionCamera != null)
        {
            companionCamera.enabled = (mode == Mode.Companion);
            if (mode == Mode.Companion)
                companionCamera.SnapToTarget();
        }
        CurrentMode = mode;
    }
}