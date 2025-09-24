using UnityEngine;
using Game.ObjectTypes;

[DisallowMultipleComponent]
public class PaintingChannelable : MonoBehaviour, IChannelable
{
    // Tracks whether this object is currently considered channeling.
    // PaintbrushChanneller calls StartChannel() every frame while aiming at a target,
    // so we make these idempotent to avoid log spam.
    private bool isChanneling;

    public void StartChannel()
    {
        if (isChanneling) return;
        isChanneling = true;
        Debug.Log($"[PaintingChannelable] Channel START on '{gameObject.name}' (instance id {GetInstanceID()}).");
    }

    public void StopChannel()
    {
        if (!isChanneling) return;
        isChanneling = false;
        Debug.Log($"[PaintingChannelable] Channel STOP on '{gameObject.name}' (instance id {GetInstanceID()}).");
    }
}