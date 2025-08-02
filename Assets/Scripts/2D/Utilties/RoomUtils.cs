using UnityEngine;
using System.Linq;
using Game.Level;

public static class RoomUtils
{
    public static IRoom GetRoomForPosition(Vector2 position, float maxDistance = 1000f)
    {
        var allRooms = GameObject.FindObjectsByType<RoomComponent>(FindObjectsSortMode.None);
        return allRooms
            .OrderBy(room => Vector2.Distance(room.Center, position))
            .FirstOrDefault(room => Vector2.Distance(room.Center, position) <= maxDistance);
    }
    public static bool IsPositionInsideRoom(Vector2 position, IRoom room) => room.Bounds.Contains(new Vector3(position.x, position.y, 0f));

}