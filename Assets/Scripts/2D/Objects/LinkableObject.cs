using UnityEngine;
using Game.ObjectTypes;

public abstract class LinkableObject : MonoBehaviour, ILinkable
{
    public string linkID;
    public string LinkID => linkID;
    public virtual string UniqueID => GetInstanceID().ToString();

    private void OnEnable() => LinkRegistry.Register(this);
    private void OnDisable() => LinkRegistry.Unregister(this);

    public abstract void OnLinkedObjectUsed(GameObject user);
}