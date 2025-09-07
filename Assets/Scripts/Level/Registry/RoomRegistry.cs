using System.Collections.Generic;
using System.Linq;
using Game.Level;
using UnityEngine;

public static class RoomRegistry
{
    private static Dictionary<int, IRoom> rooms = new Dictionary<int, IRoom>();

    public static void RegisterRoom(IRoom room)
    {
        if (!rooms.ContainsKey(room.Id))
        {
            rooms.Add(room.Id, room);
            Debug.Log($"[RoomRegistry] Registered Room {room.Id}");
        }
        else
        {
            Debug.LogWarning($"[RoomRegistry] Room ID {room.Id} already registered.");
        }
    }


    public static IRoom GetRoom(int id)
    {
        rooms.TryGetValue(id, out var room);
        return room;
    }

    //public static void LogAllRoomsAndDoors()
    //{
    //    foreach (var room in rooms.Values)
    //    {
    //        Debug.Log($"[Room {room.Id}] Connected doors: {room.ConnectedDoors.Count()}");

    //        foreach (var door in room.ConnectedDoors)
    //        {
    //            var otherRoom = door.RoomA == room ? door.RoomB : door.RoomA;
    //            Debug.Log($"  ↳ Connected to Room {otherRoom?.Id} via Door {((MonoBehaviour)door).name}");
    //        }
    //    }
    //}

}