using Game.Level;
using System.Collections.Generic;
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

}