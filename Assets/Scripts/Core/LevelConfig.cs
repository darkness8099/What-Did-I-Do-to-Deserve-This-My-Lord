using UnityEngine;

public class LevelConfig : MonoBehaviour
{
    private const int AutoCenterColumn = -1;
    private const int Lv1MaxNutrient = 10;
    private const int Lv2SeedNutrient = 11;
    private const int Lv3SeedNutrient = 21;

    [Header("Grid Size")]
    [SerializeField] private int width = 70;
    [SerializeField] private int height = 50;

    [Header("Entrance Room")]
    [SerializeField] private int entranceColumn = AutoCenterColumn;
    [SerializeField] private int entranceRowFromTop = 10;
    [SerializeField] private int openCellsBelowEntrance = 3;
    [SerializeField] private int demonLordCellsBelowEntrance = 3;

    [Header("Surface Region")]
    [SerializeField] private int surfaceBackgroundRows = 10;

    [Header("Initial Soil Nutrients")]
    [SerializeField] private StageNutrientProfile initialNutrientProfile = null;
    [SerializeField] private int initialNutrientSeed = 0;
    [SerializeField] private int initialSlimeMaxVisualIndex = 5;

    [Header("Hero Flow")]
    [SerializeField] private int currentLevelNumber = 1;
    [SerializeField] private HeroLevelConfig heroLevelSchedule;
    [SerializeField] private float heroSpawnDelaySeconds = 10f;

    private HeroLevelConfig runtimeHeroLevelFallback;

    [Header("Camera View")]
    [SerializeField] private float cameraViewColumns = 30f;
    [SerializeField] private float cameraViewRows = 16f;

    [Header("Test Tile Attributes")]
    [SerializeField] private Vector2Int[] testSlimeAttributePositions =
    {
        new Vector2Int(24, 9),
        new Vector2Int(28, 9),
        new Vector2Int(32, 9),
        new Vector2Int(36, 9),
    };

    public int Width => width;
    public int Height => height;
    public int SurfaceBackgroundRows => Mathf.Clamp(surfaceBackgroundRows, 0, height);
    public int UndergroundSurfaceY => Mathf.Clamp(height - SurfaceBackgroundRows - 1, 0, height - 1);
    public Vector2Int EntrancePosition => new Vector2Int(ResolveEntranceColumn(), ResolveEntranceY());
    public Vector2Int DemonLordStartPosition => new Vector2Int(
        ResolveEntranceColumn(),
        Mathf.Clamp(ResolveEntranceY() - demonLordCellsBelowEntrance, 0, height - 1));
    public float HeroSpawnDelaySeconds => Mathf.Max(0f, heroSpawnDelaySeconds);
    public int CurrentLevelNumber => Mathf.Max(1, currentLevelNumber);
    public float CameraViewColumns => Mathf.Max(1f, cameraViewColumns);
    public float CameraViewRows => Mathf.Max(1f, cameraViewRows);
    public Vector2 CameraStartCenter => new Vector2(width * 0.5f, height * 0.5f);

    public HeroLevelConfig GetHeroLevelConfig()
    {
        if (heroLevelSchedule != null) return heroLevelSchedule;

        string resourcePath = $"Hero/Levels/hero_level_{CurrentLevelNumber:000}";
        heroLevelSchedule = Resources.Load<HeroLevelConfig>(resourcePath);
        if (heroLevelSchedule != null) return heroLevelSchedule;

        if (runtimeHeroLevelFallback == null)
            runtimeHeroLevelFallback = HeroLevelConfig.CreateRuntimeDefault(CurrentLevelNumber, HeroSpawnDelaySeconds);
        return runtimeHeroLevelFallback;
    }

    public void ApplyInitialGrid(GridData gridData)
    {
        if (gridData == null) return;

        Vector2Int entrance = EntrancePosition;
        Vector2Int demonLord = DemonLordStartPosition;

        int surfaceY = UndergroundSurfaceY;
        for (int x = 0; x < gridData.Width; x++)
            for (int y = Mathf.Max(surfaceY + 1, 0); y < gridData.Height; y++)
                gridData.SetCell(x, y, CellType.Empty);

        ApplyInitialSoilAttributes(gridData);

        gridData.SetCell(entrance.x, entrance.y, CellType.Entrance);

        int shaftDepth = Mathf.Max(openCellsBelowEntrance, demonLordCellsBelowEntrance);
        for (int i = 1; i <= shaftDepth; i++)
        {
            int y = entrance.y - i;
            if (gridData.IsInside(entrance.x, y))
                gridData.SetCell(entrance.x, y, CellType.Empty);
        }

        gridData.SetCell(demonLord.x, demonLord.y, CellType.Empty);

        foreach (Vector2Int pos in testSlimeAttributePositions)
        {
            if (gridData.IsInside(pos.x, pos.y) && gridData.GetCell(pos.x, pos.y) == CellType.Soil)
                gridData.SetTileAttribute(pos.x, pos.y, new TileAttributeData(3, 0, TileElementType.Slime));
        }
    }

    public bool IsSurfaceLayer(int y)
    {
        return y == UndergroundSurfaceY;
    }

    public bool IsSurfaceBackgroundRow(int y)
    {
        return y > UndergroundSurfaceY && y < height;
    }

    private int ResolveEntranceColumn()
    {
        int resolved = entranceColumn == AutoCenterColumn ? width / 2 : entranceColumn;
        return Mathf.Clamp(resolved, 0, width - 1);
    }

    private int ResolveEntranceY()
    {
        int rowFromTop = Mathf.Max(1, entranceRowFromTop);
        return Mathf.Clamp(height - rowFromTop, 0, height - 1);
    }

    private void ApplyInitialSoilAttributes(GridData gridData)
    {
        int nutrientSeed = ResolveInitialNutrientSeed();
        StageNutrientProfile profile = ResolveInitialNutrientProfile(gridData, nutrientSeed);

        for (int x = 0; x < gridData.Width; x++)
        {
            for (int y = 0; y < gridData.Height; y++)
            {
                if (gridData.GetCell(x, y) != CellType.Soil) continue;

                int nutrient = GenerateInitialNutrient(x, y, profile, nutrientSeed);
                int visualIndex = TileAttributeData.GetNutrientVisualIndex(nutrient);
                TileElementType element = nutrient > 0 && visualIndex <= initialSlimeMaxVisualIndex
                    ? TileElementType.Slime
                    : TileElementType.None;

                gridData.SetTileAttribute(x, y, new TileAttributeData(nutrient, 0, element));
            }
        }
    }

    private int ResolveInitialNutrientSeed()
    {
        if (initialNutrientSeed != 0) return initialNutrientSeed;

        unchecked
        {
            int timeHash = System.DateTime.UtcNow.Ticks.GetHashCode();
            return timeHash ^ Random.Range(1, int.MaxValue);
        }
    }

    private StageNutrientProfile ResolveInitialNutrientProfile(GridData gridData, int seed)
    {
        if (HasConfiguredInitialNutrientProfile(initialNutrientProfile))
            return initialNutrientProfile;

        int surfaceY = UndergroundSurfaceY;
        NutrientClusterSettings[] clusters =
        {
            CreateDefaultStage1Cluster(gridData, surfaceY, seed, 0, 0.12f, 0.06f, 4, 6, 6, 1.1f),
            CreateDefaultStage1Cluster(gridData, surfaceY, seed, 1, 0.25f, 0.10f, 7, 10, 8, 1.15f),
            CreateDefaultStage1Cluster(gridData, surfaceY, seed, 2, 0.39f, 0.13f, 10, 13, 7, 1.15f),
            CreateDefaultStage1Cluster(gridData, surfaceY, seed, 3, 0.55f, 0.15f, 14, 17, 9, 1.2f),
            CreateDefaultStage1Cluster(gridData, surfaceY, seed, 4, 0.70f, 0.14f, 18, 21, 7, 1.15f),
            CreateDefaultStage1Cluster(gridData, surfaceY, seed, 5, 0.86f, 0.08f, 22, 26, 8, 1.2f),
            CreateDefaultStage1Cluster(gridData, surfaceY, seed, 6, 0.18f, 0.10f, 27, 31, 6, 1.1f),
            CreateDefaultStage1Cluster(gridData, surfaceY, seed, 7, 0.36f, 0.14f, 31, 35, 8, 1.2f),
            CreateDefaultStage1Cluster(gridData, surfaceY, seed, 8, 0.58f, 0.12f, 34, 38, 7, 1.15f),
            CreateDefaultStage1Cluster(gridData, surfaceY, seed, 9, 0.78f, 0.11f, 32, 37, 6, 1.1f),
        };

        return new StageNutrientProfile(InitialNutrientStage.Stage1, Lv2SeedNutrient, 0.35f, 1, 3, clusters, 6, 0);
    }

    private NutrientClusterSettings CreateDefaultStage1Cluster(
        GridData gridData,
        int surfaceY,
        int seed,
        int index,
        float baseX,
        float xJitter,
        int minDepth,
        int maxDepth,
        int power,
        float falloff)
    {
        int jitterSpan = Mathf.Max(1, Mathf.RoundToInt(gridData.Width * xJitter));
        int jitter = (PositiveHash(index, seed, 1201) % (jitterSpan * 2 + 1)) - jitterSpan;
        int x = Mathf.Clamp(Mathf.RoundToInt(gridData.Width * baseX) + jitter, 0, gridData.Width - 1);
        int depth = minDepth;
        if (maxDepth > minDepth)
            depth += PositiveHash(index, seed, 1601) % (maxDepth - minDepth + 1);
        int y = Mathf.Max(2, surfaceY - depth);
        int radiusX = 6 + PositiveHash(index, seed, 1901) % 4;
        int radiusY = 3 + PositiveHash(index, seed, 2201) % 4;
        float density = 0.72f + (PositiveHash(index, seed, 2501) % 16) / 100f;
        return new NutrientClusterSettings(new Vector2Int(x, y), radiusX, radiusY, power, falloff, density);
    }

    private bool HasConfiguredInitialNutrientProfile(StageNutrientProfile profile)
    {
        if (profile == null) return false;
        return profile.Clusters.Length > 0
            || profile.BaseScatterChance > 0f
            || profile.Lv2SeedCount > 0
            || profile.Lv3SeedCount > 0;
    }

    private int GenerateInitialNutrient(int x, int y, StageNutrientProfile profile, int seed)
    {
        if (profile == null) return 0;

        int nutrient = CalculateBaseScatterNutrient(x, y, profile, seed);
        nutrient = Mathf.Max(nutrient, CalculateClusterNutrient(x, y, profile, seed));
        nutrient = Mathf.Max(nutrient, CalculateSeedNutrient(x, y, profile, seed));
        return Mathf.Clamp(nutrient, 0, profile.MaxInitialNutrient);
    }

    private int CalculateBaseScatterNutrient(int x, int y, StageNutrientProfile profile, int seed)
    {
        if (profile.BaseScatterChance <= 0f || profile.BaseScatterMax <= 0) return 0;

        int hash = PositiveHash(x, y, 503 ^ seed);
        float roll = (hash % 10000) / 10000f;
        if (roll >= profile.BaseScatterChance) return 0;

        int range = profile.BaseScatterMax - profile.BaseScatterMin + 1;
        if (range <= 1) return profile.BaseScatterMin;

        return profile.BaseScatterMin + (PositiveHash(x, y, 907 ^ seed) % range);
    }

    private int CalculateClusterNutrient(int x, int y, StageNutrientProfile profile, int seed)
    {
        int best = 0;
        NutrientClusterSettings[] clusters = profile.Clusters;
        for (int i = 0; i < clusters.Length; i++)
        {
            NutrientClusterSettings cluster = clusters[i];
            if (cluster == null || cluster.Radius <= 0 || cluster.Power <= 0) continue;

            float normalizedDistance = GetClusterNormalizedDistance(x, y, cluster);
            if (normalizedDistance > 1f) continue;

            float centerStrength = Mathf.Pow(1f - normalizedDistance, cluster.Falloff);
            float probability = Mathf.Clamp01(cluster.Density * (0.15f + centerStrength * 0.85f));
            float roll = (PositiveHash(x, y, seed ^ (i * 4099 + 2801)) % 10000) / 10000f;
            if (roll > probability) continue;

            int minValue = Mathf.Max(1, Mathf.RoundToInt(cluster.Power * 0.25f));
            int maxValue = Mathf.Max(minValue, Mathf.RoundToInt(Mathf.Lerp(1f, cluster.Power, centerStrength)));
            int range = maxValue - minValue + 1;
            int value = minValue + (PositiveHash(x, y, seed ^ (i * 4099 + 3101)) % range);
            best = Mathf.Max(best, value);
        }

        return best;
    }

    private int CalculateSeedNutrient(int x, int y, StageNutrientProfile profile, int seed)
    {
        int seedNutrient = 0;
        if (profile.Lv2SeedCount > 0 && IsClusterSeedPoint(x, y, profile, seed, 211, profile.Lv2SeedCount))
            seedNutrient = Lv2SeedNutrient;
        if (profile.Lv3SeedCount > 0 && IsClusterSeedPoint(x, y, profile, seed, 337, profile.Lv3SeedCount))
            seedNutrient = Mathf.Max(seedNutrient, Lv3SeedNutrient);
        return seedNutrient;
    }

    private bool IsClusterSeedPoint(int x, int y, StageNutrientProfile profile, int seed, int salt, int desiredCount)
    {
        NutrientClusterSettings[] clusters = profile.Clusters;
        if (clusters.Length == 0) return false;

        int placed = 0;
        int attempts = Mathf.Max(desiredCount * 24, 48);
        for (int i = 0; i < attempts && placed < desiredCount; i++)
        {
            NutrientClusterSettings cluster = clusters[PositiveHash(i, seed, salt) % clusters.Length];
            int sx = PickClusterSeedCoordinate(cluster.Center.x, cluster.RadiusX, i, seed, salt ^ 101);
            int sy = PickClusterSeedCoordinate(cluster.Center.y, cluster.RadiusY, i, seed, salt ^ 307);
            if (GetClusterNormalizedDistance(sx, sy, cluster) > 0.55f) continue;
            if (CalculateClusterNutrient(sx, sy, profile, seed) <= 0) continue;

            placed++;
            if (x == sx && y == sy)
                return true;
        }

        return false;
    }

    private int PickClusterSeedCoordinate(int center, int radius, int index, int seed, int salt)
    {
        int safeRadius = Mathf.Max(0, radius);
        int span = safeRadius * 2 + 1;
        if (span <= 1) return center;
        return center + (PositiveHash(index, seed, salt) % span) - safeRadius;
    }

    private float GetClusterNormalizedDistance(int x, int y, NutrientClusterSettings cluster)
    {
        if (cluster.RadiusX <= 0 || cluster.RadiusY <= 0) return float.PositiveInfinity;

        float dx = (x - cluster.Center.x) / (float)cluster.RadiusX;
        float dy = (y - cluster.Center.y) / (float)cluster.RadiusY;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    private int PositiveHash(int x, int y, int salt)
    {
        return Mathf.Abs((x * 73856093) ^ (y * 19349663) ^ (salt * 83492791));
    }
}
