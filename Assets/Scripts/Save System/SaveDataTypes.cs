// Inside one file: SaveDataTypes.cs
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameSaveData
{
    // Movement & Look Rotation
    public Vector3 position;
    public float bodyYaw;
    public float cameraPitch;

    // Collectibles
    public List<string> collectedIds;

    // State Machine
    public PlayerStateMachine.PlayerStateKind playerState;

    // Flashlight 
    public FlashlightSaveData flashlight;

    public DrawerSaveData drawerState;

    public LockedDoorSaveData lockeddoorState;

    public DialogueSaveData dialogue;

    public EnemyIntroSaveData enemyIntro;

    public HintSaveData hint;

    public TutorialSaveData tutorial;

    public List<EnemySaveData> savedEnemies = new List<EnemySaveData>();

    public EnemyManagerSaveData enemyManagerData;

    public PaintbrushSaveData paintbrush;

    public PaintingRandomizerSaveData randomizedPaintings;

    public List<SpatialSFXSaveData> spatialSFXStates;

    public List<SpatialTriggerSaveData> spatialTriggerStates;

    public List<BatterySaveData> batteryStates;

    public ElevatorAttackSaveData elevatorAttack;
}

[System.Serializable]
public class FlashlightSaveData
{
    public float currentBattery;
    public bool isOn;
}

[System.Serializable]
public class DrawerSaveData
{
    public bool hasDrawerOpened;
}

[System.Serializable]
public class LockedDoorSaveData
{
    public bool hadDoorOpened;
}

[System.Serializable]
public class DialogueSaveData
{
    public List<int> triggeredDialogueIds;
}

[System.Serializable]
public class EnemyIntroSaveData
{
    public bool hasTriggered;
}

[System.Serializable]
public class HintSaveData
{
    public List<int> triggeredHintIds;

    public int savedCorruptedPaintingsChanneled;
}

[System.Serializable]
public class TutorialSaveData
{
    public List<int> triggeredTutorialIDs;
}

[System.Serializable]
public class EnemySaveData
{
    public string enemyGameObjectName;
    public Vector3 position;
    public bool isEnemyActivated;
    public int corruptedPaintingsChanneled;
}

[System.Serializable]
public class EnemyManagerSaveData
{
    public bool systemIsActivated;
    public int currentIntervalIndex;
    public float checkTimer;
    public int currentTriggerCount;
    public string activeGhostName;
}

[System.Serializable]
public class PaintbrushSaveData
{
    public List<string> completedPaintingIds;
}

[System.Serializable]
public class PaintingRandomizerSaveData
{
    // Legacy field retained so saves created before room-local indices still load.
    public List<string> savedPaintingNames;

    // One index per room. Unlike GameObject names, these remain unambiguous when
    // multiple painting prefabs use the same imported object name.
    public List<int> savedPaintingIndices;
}

[System.Serializable]
public class SpatialSFXSaveData
{
    public string id;
    public bool isActive;
}

[System.Serializable]
public class SpatialTriggerSaveData
{
    public string id;
    public bool isActive;
}

[System.Serializable]
public class BatterySaveData
{
    public string id;
    public bool isUsed;
    public bool onboardingRevealed;
}

[System.Serializable]
public class ElevatorAttackSaveData
{
    public bool hasPlayed;
}
