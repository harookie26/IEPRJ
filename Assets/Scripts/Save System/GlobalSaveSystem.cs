using Game.States;
using System.Collections.Generic;
using UnityEngine;

public static class GlobalSaveSystem
{
    private static readonly List<ISaveable> saveables = new();

    public static void Register(ISaveable s)
    {
        if (!saveables.Contains(s))
            saveables.Add(s);
    }

    public static void Unregister(ISaveable s)
    {
        saveables.Remove(s);
    }

    public static void CaptureInto(GameSaveData data)
    {
        if (data == null) return;

        data.spatialSFXStates = new List<SpatialSFXSaveData>();

        foreach (var s in saveables)
        {
            if (s is SpatialSFX sfx)
            {
                Debug.Log($"[Global Save System] Capturing state for SpatialSFX: {sfx.SaveKey}");

                data.spatialSFXStates.Add(
                    (SpatialSFXSaveData)sfx.CaptureState()
                );
            }

            if (s is SpatialTrigger stx)
            {
                Debug.Log($"[Global Save System] Capturing state for SpatialTrigger: {stx.SaveKey}");
                data.spatialTriggerStates.Add(
                    (SpatialTriggerSaveData)stx.CaptureState()
                );
            }
        }
    }

    public static void RestoreFrom(GameSaveData data)
    {
        if (data?.spatialSFXStates == null) return;

        foreach (var s in saveables)
        {
            if (s is SpatialSFX sfx)
            {
                foreach (var state in data.spatialSFXStates)
                {
                    if (state.id == sfx.SaveKey)
                    {
                        Debug.Log($"[Global Save System] Restoring state for SpatialSFX: {sfx.SaveKey}");

                        sfx.RestoreState(state);
                        break;
                    }
                }
            }
        }
    }

    public static List<ISaveable> All => saveables;
}