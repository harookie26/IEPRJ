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
        data.spatialTriggerStates = new List<SpatialTriggerSaveData>();
        data.batteryStates = new List<BatterySaveData>();
        data.savedEnemies = new List<EnemySaveData>();

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

            if (s is BatteryComponent battery)
            {
                data.batteryStates.Add(
                    (BatterySaveData)battery.CaptureState()
                );
            }

            if (s is ElevatorAttackCutscene elevatorAttack)
            {
                data.elevatorAttack =
                    (ElevatorAttackSaveData)elevatorAttack.CaptureState();
            }

            if (s is EnemyManager manager)
            {
                data.enemyManagerData = (EnemyManagerSaveData)manager.CaptureState();
            }
            else if (s is EnemyStateMachine ghost)
            {
                EnemySaveData ghostData = (EnemySaveData)ghost.CaptureState();
                ghostData.enemyGameObjectName = ghost.SaveKey; // Matches the key cleanly
                data.savedEnemies.Add(ghostData);
            }
        }
    }

    public static void RestoreFrom(GameSaveData data)
    {
        if (data == null) return;

        foreach (var s in saveables)
        {
            if (s is EnemyStateMachine ghost && data.savedEnemies != null)
            {
                foreach (var ghostData in data.savedEnemies)
                {
                    if (ghostData.enemyGameObjectName == ghost.SaveKey)
                    {
                        ghost.RestoreState(ghostData);
                        break;
                    }
                }
            }
        }

        if (data.enemyManagerData != null)
        {
            foreach (var s in saveables)
            {
                if (s is EnemyManager manager)
                {
                    manager.LoadSaveData(data.enemyManagerData);
                    break;
                }
            }
        }

        foreach (var s in saveables)
        {
            if (s is SpatialSFX sfx && data.spatialSFXStates != null)
            {
                foreach (var state in data.spatialSFXStates)
                {
                    if (state.id == sfx.SaveKey) { sfx.RestoreState(state); break; }
                }
            }

            if (s is SpatialTrigger stx && data.spatialTriggerStates != null)
            {
                foreach (var state in data.spatialTriggerStates)
                {
                    if (state.id == stx.SaveKey) { stx.RestoreState(state); break; }
                }
            }

            if (s is BatteryComponent battery && data.batteryStates != null)
            {
                foreach (var state in data.batteryStates)
                {
                    if (state.id == battery.SaveKey) { battery.RestoreState(state); break; }
                }
            }

            if (s is ElevatorAttackCutscene elevatorAttack)
            {
                elevatorAttack.RestoreState(data.elevatorAttack);
            }
        }
    }

    public static List<ISaveable> All => saveables;
}
