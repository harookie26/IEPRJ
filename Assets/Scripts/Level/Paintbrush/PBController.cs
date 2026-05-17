using UnityEngine;

[FoldableInspector]
public class PBController : MonoBehaviour
{
    public enum Mode { Follow, Manual }
    public Mode CurrentMode { get; private set; } = Mode.Follow;

    [SerializeField] private PBFollow pbFollow;
    [SerializeField] private PBManual pbManual;

    public static bool IsCompanionManualModeActive { get; private set; } = false;

    private void Awake()
    {
        if (pbFollow == null)
            pbFollow = GetComponent<PBFollow>();
        if (pbManual == null)
            pbManual = GetComponent<PBManual>();
    }

    public void OnGameRestartReset()
    {
        SetMode(Mode.Follow);
    }

    private void Start()
    {
        SetMode(CurrentMode);
    }

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Tab))
        //    ToggleMode();
    }

    private void ToggleMode()
    {
        SetMode(CurrentMode == Mode.Follow ? Mode.Manual : Mode.Follow);
    }

    public void SetMode(Mode mode)
    {
        CurrentMode = mode;
        IsCompanionManualModeActive = (mode == Mode.Manual);

        if (pbFollow != null)
            pbFollow.enabled = (mode == Mode.Follow);

        if (pbManual != null)
        {
            if (mode == Mode.Follow)
            {
                // BYPASS UNITY QUIRK: Force the player to unglue right now, 
                // regardless of whether the GameObject is active or inactive.
                pbManual.ForceReleasePlayer();
            }

            pbManual.enabled = (mode == Mode.Manual);
        }
    }
}