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

    public LockedDoorSaveData lockeddoorState;

    public DialogueSaveData dialogue;

    public EnemyIntroSaveData enemyIntro;

    public HintSaveData hint;

    public TutorialSaveData tutorial;

    public EnemySaveData enemyState;

    public PaintbrushSaveData paintbrush;

    public PaintingRandomizerSaveData randomizedPaintings;

    public List<SpatialSFXSaveData> spatialSFXStates;
}

[System.Serializable]
public class FlashlightSaveData
{
    public float currentBattery;
    public bool isOn;
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
    public Vector3 position;
    public bool isEnemyActivated;
    public int corruptedPaintingsChanneled;
}

[System.Serializable]
public class PaintbrushSaveData
{
    public List<string> completedPaintingIds;
}

[System.Serializable]
public class PaintingRandomizerSaveData
{
    // Because names are unique now, this will never fail!
    public List<string> savedPaintingNames;
}

[System.Serializable]
public class SpatialSFXSaveData
{
    public string id;
    public bool isActive;
}