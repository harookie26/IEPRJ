using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCutsceneSequence", menuName = "Cutscene/Sequence")]
public class CutsceneSequence : ScriptableObject
{
    [SerializeReference]
    public List<CutsceneAction> actions = new List<CutsceneAction>();
}