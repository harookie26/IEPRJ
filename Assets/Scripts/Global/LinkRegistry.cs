using System.Collections.Generic;
using System.Linq;
using Game.ObjectTypes;

public static class LinkRegistry
{
    private static Dictionary<string, List<ILinkable>> registry = new();

    public static void Clear() => registry.Clear();

    public static void Register(ILinkable obj)
    {
        if (obj == null || string.IsNullOrEmpty(obj.LinkID)) return;

        if (!registry.ContainsKey(obj.LinkID))
            registry[obj.LinkID] = new List<ILinkable>();

        if (!registry[obj.LinkID].Exists(o => o.UniqueID == obj.UniqueID))
        {
            registry[obj.LinkID].Add(obj);
        }
    }


    public static void Unregister(ILinkable obj)
    {
        if (obj == null || string.IsNullOrEmpty(obj.LinkID)) return;

        if (registry.TryGetValue(obj.LinkID, out var list))
        {
            list.RemoveAll(o =>
                o == null || o.UniqueID == obj.UniqueID ||
                (o is UnityEngine.Object uo && uo == null));

            if (list.Count == 0)
                registry.Remove(obj.LinkID);
        }
    }


    public static List<ILinkable> GetLinkedObjects(string id)
    {
        if (!registry.TryGetValue(id, out var list))
            return new List<ILinkable>();

        // Remove nulls or destroyed Unity objects
        list.RemoveAll(obj =>
        {
            if (obj is UnityEngine.Object unityObj && unityObj == null)
                return true;
            return obj == null;
        });

        return list;
    }

}
