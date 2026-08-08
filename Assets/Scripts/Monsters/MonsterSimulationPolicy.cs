using System.Collections.Generic;
using UnityEngine;

public enum MonsterSimulationTier
{
    Exact,
    Reduced,
    Aggregate,
}

public struct MonsterSimulationInterest
{
    public RectInt ExactRect;
    public RectInt ReducedRect;
    public IList<Vector2Int> HeroPositions;
    public int HeroExactRadius;
    public int HeroReducedRadius;
}

public static class MonsterSimulationPolicy
{
    public static RectInt GetCameraCellRect(Camera camera, int paddingCells)
    {
        if (camera == null) return new RectInt();

        float halfHeight = camera.orthographicSize;
        float halfWidth = halfHeight * camera.aspect;
        Vector3 center = camera.transform.position;
        int padding = Mathf.Max(0, paddingCells);

        int xMin = Mathf.FloorToInt(center.x - halfWidth) - padding;
        int xMax = Mathf.CeilToInt(center.x + halfWidth) + padding;
        int yMin = Mathf.FloorToInt(center.y - halfHeight) - padding;
        int yMax = Mathf.CeilToInt(center.y + halfHeight) + padding;
        return new RectInt(xMin, yMin, Mathf.Max(0, xMax - xMin), Mathf.Max(0, yMax - yMin));
    }

    public static MonsterSimulationInterest BuildInterest(
        Camera camera,
        int exactPaddingCells,
        int reducedPaddingCells,
        IList<Vector2Int> heroPositions,
        int heroExactRadius,
        int heroReducedRadius)
    {
        RectInt visible = GetCameraCellRect(camera, 0);
        return new MonsterSimulationInterest
        {
            ExactRect = Expand(visible, exactPaddingCells),
            ReducedRect = Expand(visible, Mathf.Max(exactPaddingCells, reducedPaddingCells)),
            HeroPositions = heroPositions,
            HeroExactRadius = Mathf.Max(0, heroExactRadius),
            HeroReducedRadius = Mathf.Max(heroExactRadius, heroReducedRadius),
        };
    }

    public static MonsterSimulationTier Classify(Vector2Int position, MonsterSimulationInterest interest)
    {
        if (interest.ExactRect.Contains(position) || IsNearAnyHero(position, interest.HeroPositions, interest.HeroExactRadius))
            return MonsterSimulationTier.Exact;

        if (interest.ReducedRect.Contains(position) || IsNearAnyHero(position, interest.HeroPositions, interest.HeroReducedRadius))
            return MonsterSimulationTier.Reduced;

        return MonsterSimulationTier.Aggregate;
    }

    public static MonsterSimulationTier ClassifyRegion(RectInt regionBounds, MonsterSimulationInterest interest)
    {
        if (regionBounds.Overlaps(interest.ExactRect) ||
            IsRegionNearAnyHero(regionBounds, interest.HeroPositions, interest.HeroExactRadius))
            return MonsterSimulationTier.Exact;

        if (regionBounds.Overlaps(interest.ReducedRect) ||
            IsRegionNearAnyHero(regionBounds, interest.HeroPositions, interest.HeroReducedRadius))
            return MonsterSimulationTier.Reduced;

        return MonsterSimulationTier.Aggregate;
    }

    public static RectInt Expand(RectInt rect, int cells)
    {
        int p = Mathf.Max(0, cells);
        return new RectInt(rect.xMin - p, rect.yMin - p, rect.width + p * 2, rect.height + p * 2);
    }

    private static bool IsNearAnyHero(Vector2Int position, IList<Vector2Int> heroes, int radius)
    {
        if (heroes == null) return false;
        for (int i = 0; i < heroes.Count; i++)
        {
            Vector2Int hero = heroes[i];
            if (Mathf.Abs(position.x - hero.x) + Mathf.Abs(position.y - hero.y) <= radius)
                return true;
        }
        return false;
    }

    private static bool IsRegionNearAnyHero(RectInt region, IList<Vector2Int> heroes, int radius)
    {
        if (heroes == null) return false;
        for (int i = 0; i < heroes.Count; i++)
        {
            Vector2Int hero = heroes[i];
            int closestX = Mathf.Clamp(hero.x, region.xMin, region.xMax - 1);
            int closestY = Mathf.Clamp(hero.y, region.yMin, region.yMax - 1);
            if (Mathf.Abs(closestX - hero.x) + Mathf.Abs(closestY - hero.y) <= radius)
                return true;
        }
        return false;
    }
}
