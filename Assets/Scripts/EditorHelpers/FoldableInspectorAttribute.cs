using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class FoldableInspectorAttribute : Attribute
{
    public string DisplayName { get; }
    public bool HideFieldHeaders { get; }

    public FoldableInspectorAttribute(string displayName = "", bool hideFieldHeaders = true)
    {
        DisplayName = displayName;
        HideFieldHeaders = hideFieldHeaders;
    }
}