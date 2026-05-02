using System;

/// <summary>
/// Runtime attribute used to opt-in MonoBehaviours to the foldable inspector available in the Editor.
/// This is intentionally kept in runtime so editor scripts can read the attribute via reflection.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class FoldableInspectorAttribute : Attribute
{
    public bool HideFieldHeaders { get; }
    public string DisplayName { get; }

    public FoldableInspectorAttribute(bool hideFieldHeaders = false, string displayName = null)
    {
        HideFieldHeaders = hideFieldHeaders;
        DisplayName = displayName;
    }
}
