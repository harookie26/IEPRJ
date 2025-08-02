using System.Collections.Generic;
using System.Linq;
using Game.Level;
using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class PathfinderComponent : MonoBehaviour
{
    public IRoom CurrentRoom { get; set; }
    public IRoom TargetRoom { get; private set; }

    [SerializeField] private int targetRoomId;
    [SerializeField] private float moveSpeed = 2f;

    private List<IRoom> roomPath;
    private int roomPathIndex = 0;
    private Doors currentDoorTarget;

    private float teleportCooldown = 1f;
    private float lastTeleportTime = -999f;

    public Doors LastUsedDoor { get; private set; }

    private void Start()
    {
        if (!CompareTag("Pathfinding"))
            Debug.LogWarning("[Pathfinding] This GameObject should be tagged as 'Pathfinding'");

        CurrentRoom = RoomUtils.GetRoomForPosition(transform.position);
        if (CurrentRoom == null)
        {
            Debug.LogError("[Pathfinding] Could not detect starting room.");
            return;
        }

        if (targetRoomId != 0)
        {
            var target = RoomRegistry.GetRoom(targetRoomId);
            if (target != null)
                SetTargetRoom(target);
        }
    }

    public Doors CurrentDoorTarget => currentDoorTarget;

    public bool CanTeleportFrom(Doors door) => door != LastUsedDoor || Time.time - lastTeleportTime >= teleportCooldown;

    private Doors FindDoorTo(IRoom fromRoom, IRoom toRoom)
    {
        var allDoors = GameObject.FindObjectsByType<Doors>(FindObjectsSortMode.None);

        foreach (var door in allDoors)
        {
            if (!RoomUtils.IsPositionInsideRoom(door.transform.position, fromRoom))
                continue;

            var linkedDoors = LinkRegistry.GetLinkedObjects(door.LinkID);

            foreach (var linkObj in linkedDoors)
            {
                if (linkObj is not Doors linked) continue;
                if (linked == door) continue;

                if (RoomUtils.IsPositionInsideRoom(linked.transform.position, toRoom))
                    return door;
               
            }
        }

        Debug.LogWarning($"[FindDoorTo] No door inside Room {fromRoom.Id} links to Room {toRoom.Id}");
        return null;
    }

    public void SetTargetRoom(IRoom target)
    {
        if (target == null) return;

        TargetRoom = target;
        roomPath = RoomPathfinder.FindRoomPath(CurrentRoom, TargetRoom);
        roomPathIndex = 0;

        if (roomPath == null || roomPath.Count == 0)
        {
            Debug.LogWarning($"[EnemyAI] No path found from Room {CurrentRoom.Id} to Room {TargetRoom.Id}");
            return;
        }

        string pathLog = "[EnemyAI] Full Room Path: ";
        for (int i = 0; i < roomPath.Count; i++)
        {
            pathLog += $"Room {roomPath[i].Id}";
            if (i < roomPath.Count - 1)
                pathLog += " → ";
        }
        Debug.Log(pathLog);
    }

    public void RegisterTeleport(Doors usedDoor, IRoom enteredRoom)
    {
        LastUsedDoor = usedDoor;
        lastTeleportTime = Time.time;
        CurrentRoom = enteredRoom;

        Debug.Log($"[EnemyAI] Entered Room {enteredRoom.Id} via {usedDoor.name}");

        foreach (var door in enteredRoom.ConnectedDoors)
        {
            var doorName = ((MonoBehaviour)door).name;
            var leadsTo = door.RoomA == enteredRoom ? door.RoomB : door.RoomA;
            var leadsToId = leadsTo?.Id.ToString() ?? "null";

        }

        if (roomPathIndex + 1 < roomPath.Count && roomPath[roomPathIndex + 1] == enteredRoom
            roomPathIndex++;
        else
            Debug.LogWarning($"[EnemyAI] Entered unexpected room {enteredRoom.Id}. Expected: {roomPath[roomPathIndex + 1].Id}");
    }

    private void Update()
    {
        if (roomPath == null || roomPathIndex >= roomPath.Count) return;

        var nextRoom = roomPathIndex + 1 < roomPath.Count ? roomPath[roomPathIndex + 1] : null;
        if (CurrentRoom == nextRoom)
        {
            roomPathIndex++;
            nextRoom = roomPathIndex + 1 < roomPath.Count ? roomPath[roomPathIndex + 1] : null;
        }

        if (nextRoom == null) return;

        currentDoorTarget = FindDoorTo(CurrentRoom, nextRoom);
        if (currentDoorTarget == null)
        {
            Debug.LogWarning($"[Pathfinding] No door found from Room {CurrentRoom?.Id} to Room {nextRoom?.Id}");
            return;
        }

        var doorPos = currentDoorTarget.GetEntryPointFor(CurrentRoom);
        transform.position = Vector2.MoveTowards(transform.position, doorPos, Time.deltaTime * moveSpeed);
        Debug.DrawLine(transform.position, doorPos, Color.red);
    }


    private void OnDrawGizmos()
    {
        if (roomPath == null || roomPath.Count == 0) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < roomPath.Count - 1; i++)
        {
            Gizmos.DrawLine(roomPath[i].Bounds.center, roomPath[i + 1].Bounds.center);
        }

        if (TargetRoom != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere((Vector3)TargetRoom.Center, 0.5f);
        }
    }
}
