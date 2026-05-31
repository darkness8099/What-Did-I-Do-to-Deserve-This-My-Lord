using UnityEngine;

public enum InitialNutrientStage
{
    Stage1 = 1,
    Stage2 = 2,
    Stage3Plus = 3
}

[System.Serializable]
public class NutrientClusterSettings
{
    [SerializeField] private Vector2Int center = Vector2Int.zero;
    [SerializeField] private int radiusX = 5;
    [SerializeField] private int radiusY = 4;
    [SerializeField] private int power = 5;
    [SerializeField] private float falloff = 1f;
    [SerializeField] private float density = 0.85f;

    public NutrientClusterSettings()
    {
    }

    public NutrientClusterSettings(Vector2Int center, int radius, int power, float falloff)
        : this(center, radius, radius, power, falloff, 0.85f)
    {
    }

    public NutrientClusterSettings(Vector2Int center, int radiusX, int radiusY, int power, float falloff, float density)
    {
        this.center = center;
        this.radiusX = radiusX;
        this.radiusY = radiusY;
        this.power = power;
        this.falloff = falloff;
        this.density = density;
    }

    public Vector2Int Center => center;
    public int Radius => Mathf.Max(RadiusX, RadiusY);
    public int RadiusX => Mathf.Max(0, radiusX);
    public int RadiusY => Mathf.Max(0, radiusY);
    public int Power => Mathf.Max(0, power);
    public float Falloff => Mathf.Max(0.01f, falloff);
    public float Density => Mathf.Clamp01(density);
}

[System.Serializable]
public class StageNutrientProfile
{
    [SerializeField] private InitialNutrientStage stage = InitialNutrientStage.Stage1;
    [SerializeField] private int maxInitialNutrient = 10;
    [SerializeField] private float baseScatterChance = 0f;
    [SerializeField] private int baseScatterMin = 1;
    [SerializeField] private int baseScatterMax = 3;
    [SerializeField] private NutrientClusterSettings[] clusters = new NutrientClusterSettings[0];
    [SerializeField] private int lv2SeedCount = 0;
    [SerializeField] private int lv3SeedCount = 0;

    public InitialNutrientStage Stage => stage;
    public int MaxInitialNutrient => Mathf.Max(0, maxInitialNutrient);
    public float BaseScatterChance => Mathf.Clamp01(baseScatterChance);
    public int BaseScatterMin => Mathf.Max(0, Mathf.Min(baseScatterMin, baseScatterMax));
    public int BaseScatterMax => Mathf.Max(BaseScatterMin, baseScatterMax);
    public NutrientClusterSettings[] Clusters => clusters ?? new NutrientClusterSettings[0];
    public int Lv2SeedCount => AllowsLv2Seeds ? Mathf.Max(0, lv2SeedCount) : 0;
    public int Lv3SeedCount => AllowsLv3Seeds ? Mathf.Max(0, lv3SeedCount) : 0;
    public bool AllowsLv2Seeds => stage != InitialNutrientStage.Stage1 || lv2SeedCount > 0;
    public bool AllowsLv3Seeds => stage == InitialNutrientStage.Stage3Plus;

    public StageNutrientProfile()
    {
    }

    public StageNutrientProfile(
        InitialNutrientStage stage,
        int maxInitialNutrient,
        float baseScatterChance,
        int baseScatterMin,
        int baseScatterMax,
        NutrientClusterSettings[] clusters,
        int lv2SeedCount,
        int lv3SeedCount)
    {
        this.stage = stage;
        this.maxInitialNutrient = maxInitialNutrient;
        this.baseScatterChance = baseScatterChance;
        this.baseScatterMin = baseScatterMin;
        this.baseScatterMax = baseScatterMax;
        this.clusters = clusters ?? new NutrientClusterSettings[0];
        this.lv2SeedCount = lv2SeedCount;
        this.lv3SeedCount = lv3SeedCount;
    }
}
