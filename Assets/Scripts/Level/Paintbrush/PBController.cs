using UnityEngine;

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

    private void Start()
    {
        SetMode(CurrentMode);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            ToggleMode();
    }

    private void ToggleMode()
    {
        CurrentMode = (CurrentMode == Mode.Follow) ? Mode.Manual : Mode.Follow;
        SetMode(CurrentMode);
    }

    private void SetMode(Mode mode)
    {
        if (pbFollow != null)
            pbFollow.enabled = (mode == Mode.Follow);
        if (pbManual != null)
            pbManual.enabled = (mode == Mode.Manual);

        IsCompanionManualModeActive = (mode == Mode.Manual);
    }
}