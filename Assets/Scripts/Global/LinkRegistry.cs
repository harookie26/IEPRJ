using System.Collections.Generic;
using System.Linq;
using Game.ObjectTypes;

public static class LinkRegistry
{
    private static Dictionary<string, List<ILinkable>> registry = new();

    public static void Register(ILinkable obj)
    {
        if (!registry.ContainsKey(obj.LinkID))
            registry[obj.LinkID] = new List<ILinkable>();

        if (!registry[obj.LinkID].Exists(o => o.UniqueID == obj.UniqueID))
            registry[obj.LinkID].Add(obj);
    }

    public static void Unregister(ILinkable obj)
    {
        if (registry.TryGetValue(obj.LinkID, out var list))
        {
            list.RemoveAll(o => o.UniqueID == obj.UniqueID);
            if (list.Count == 0)
                registry.Remove(obj.LinkID);
        }
    }

    public static List<ILinkable> GetLinkedObjects(string id)
    {
        return registry.TryGetValue(id, out var list) ? list : new List<ILinkable>();
    }
}
