using Game.States;
using UnityEngine;

[DisallowMultipleComponent]
[FoldableInspector]
public class BatteryComponent : MonoBehaviour, ISaveable
{
    [Header("Battery")]
    [SerializeField] private string batteryId;
    [SerializeField, Min(0.1f)] private float requiredReplacementDuration = 2f;
    [SerializeField, Range(0f, 100f)] private float refillPercent = 100f;
    [SerializeField] private bool consumeOnUse = true;

    private bool isUsed;

    public string SaveKey => string.IsNullOrWhiteSpace(batteryId) ? GetHierarchyPath() : batteryId;
    public float RequiredReplacementDuration => requiredReplacementDuration;
    public bool CanUse => isActiveAndEnabled && !isUsed;

    private void Awake()
    {
        GlobalSaveSystem.Register(this);
        ApplyUsedState();
    }

    private void OnDestroy()
    {
        GlobalSaveSystem.Unregister(this);
    }

    public bool TryUse(Flashlight flashlight)
    {
        if (!CanUse || flashlight == null)
            return false;

        flashlight.RefillBattery(refillPercent);

        if (consumeOnUse)
        {
            isUsed = true;
            ApplyUsedState();
        }

        return true;
    }

    public object CaptureState()
    {
        return new BatterySaveData
        {
            id = SaveKey,
            isUsed = isUsed
        };
    }

    public void RestoreState(object state)
    {
        if (state is not BatterySaveData data)
            return;

        isUsed = data.isUsed;
        ApplyUsedState();
    }

    private void ApplyUsedState()
    {
        if (consumeOnUse && isUsed && gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private string GetHierarchyPath()
    {
        string path = gameObject.name;
        Transform current = transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return gameObject.scene.name + "/" + path;
    }
}
