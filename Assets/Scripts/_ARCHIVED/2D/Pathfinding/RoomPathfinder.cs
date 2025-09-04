using System.Collections.Generic;
using UnityEngine;
using Game.Level;

public struct RoomStep
{
    public IRoom FromRoom;
    public IRoom ToRoom;

    public RoomStep(IRoom from, IRoom to)
    {
        FromRoom = from;
        ToRoom = to;
    }
}

public static class RoomPathfinder
{
    public static List<IRoom> FindRoomPath(IRoom start, IRoom goal)
    {
        var openSet = new PriorityQueue<IRoom>();
        var cameFrom = new Dictionary<IRoom, IRoom>();
        var gScore = new Dictionary<IRoom, float>();
        var fScore = new Dictionary<IRoom, float>();
        var visited = new HashSet<IRoom>();

        openSet.Enqueue(start, 0);
        gScore[start] = 0;
        fScore[start] = Heuristic(start, goal);

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            if (current == goal)
                return ReconstructPath(cameFrom, goal);

            visited.Add(current);

            foreach (var door in current.ConnectedDoors)
            {
                var neighbor = door.RoomA == current ? door.RoomB : door.RoomA;
                if (neighbor == null || visited.Contains(neighbor)) continue;

                float tentativeG = gScore[current] + Vector2.Distance(current.Center, neighbor.Center) * door.Weight;

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                    if (!openSet.Contains(neighbor))
                        openSet.Enqueue(neighbor, fScore[neighbor]);
                    else
                        openSet.UpdatePriority(neighbor, fScore[neighbor]);
                }
            }
        }

        Debug.LogWarning("[RoomPathfinder] No path found.");
        return new List<IRoom>();
    }

    private static float Heuristic(IRoom a, IRoom b)
    {
        var boundsA = a.Bounds;
        var boundsB = b.Bounds;

        Vector3 closestA = boundsA.ClosestPoint(boundsB.center);
        Vector3 closestB = boundsB.ClosestPoint(boundsA.center);

        return Vector3.Distance(closestA, closestB);
    }


    private static List<IRoom> ReconstructPath(Dictionary<IRoom, IRoom> cameFrom, IRoom current)
    {
        var path = new List<IRoom> { current };

        while (cameFrom.TryGetValue(current, out var from))
        {
            path.Insert(0, from);
            current = from;
        }

        return path;
    }
}