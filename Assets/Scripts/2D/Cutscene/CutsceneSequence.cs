using System.Collections.Generic;
using UnityEngine; // Still needed for [SerializeReference] if you ever mix workflows
using System;

// No longer a ScriptableObject, just a plain C# class
[Serializable]
public class CutsceneSequence
{
    // This attribute is for Unity's serializer, but is harmless here.
    // YamlDotNet uses the public property name.
    [SerializeReference]
    public List<CutsceneAction> actions = new List<CutsceneAction>();
}