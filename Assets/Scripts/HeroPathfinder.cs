using System.Collections.Generic;
using UnityEngine;

public class HeroPathfinder
{
    private readonly GridData gridData;

    private static readonly Vector2Int[] Directions = new Vector2Int[]
    {
        new Vector2Int( 0,  1),
        new Vector2Int( 0, -1),
        new Vector2Int(-1,  0),
        new Vector2Int( 1,  0),
    };

    public HeroPathfinder(GridData gridData)
    {
        this.gridData = gridData;
    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        if (!gridData.IsInside(start.x, start.y)) return null;
        if (!gridData.IsInside(goal.x, goal.y))   return null;
        if (!IsWalkable(start)) return null;
        if (!IsWalkable(goal))  return null;

        if (start == goal)
            return new List<Vector2Int> { start };

        var queue   = new Queue<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        var parent  = new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (current == goal)
                return ReconstructPath(parent, start, goal);

            foreach (Vector2Int dir in Directions)
            {
                Vector2Int next = current + dir;
                if (!gridData.IsInside(next.x, next.y)) continue;
                if (visited.Contains(next))             continue;
                if (!IsWalkable(next))                  continue;

                visited.Add(next);
                parent[next] = current;
                queue.Enqueue(next);
            }
        }

        return null;
    }

    private bool IsWalkable(Vector2Int pos)
    {
        CellType type = gridData.GetCell(pos.x, pos.y);
        return type == CellType.Empty
            || type == CellType.Entrance;
    }

    private static List<Vector2Int> ReconstructPath(
        Dictionary<Vector2Int, Vector2Int> parent,
        Vector2Int start,
        Vector2Int goal)
    {
        var path    = new List<Vector2Int>();
        Vector2Int current = goal;

        while (current != start)
        {
            path.Add(current);
            current = parent[current];
        }
        path.Add(start);
        path.Reverse();
        return path;
    }
}
