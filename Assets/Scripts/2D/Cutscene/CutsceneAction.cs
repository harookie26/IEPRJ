using System;

[Serializable]
public abstract class CutsceneAction
{
    public abstract void Execute(Action onComplete);
}