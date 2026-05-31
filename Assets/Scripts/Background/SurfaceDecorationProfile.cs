using System.Collections.Generic;

// Categories used by surface decoration (see SURFACE_DECORATION_RULES § 四).
public enum DecorationCategory
{
    Background,
    Entrance,
    SurfaceObject,
    Building,
    Prop,
    Vegetation,
}

// Surface zone identity (see SURFACE_DECORATION_RULES § 三).
public enum DecorationZone
{
    A_LeftNature,
    B_LeftVillage,
    C_CenterEntrance,
    D_RightVillage,
    E_RightLandmark,
}

// Half-open [StartX, EndX) horizontal range owned by a zone.
public struct ZoneBounds
{
    public DecorationZone Zone;
    public int StartX;
    public int EndX;
    public int Width => EndX - StartX;
    public bool Contains(int x) => x >= StartX && x < EndX;
}

// Plain class config for surface decoration (per SURFACE_DECORATION_RULES § 八, plain class first; may migrate to ScriptableObject later).
// Carries: zone boundaries, category×zone weights, available sprites per category, footprint widths.
// Does NOT carry placement logic — that's SurfaceDecorationSpawner's job (TASK-045).
public class SurfaceDecorationProfile
{
    public int SurfaceWidth { get; private set; }
    public int SurfaceHeight { get; private set; }
    public int EntranceCenterX { get; private set; }

    private List<ZoneBounds> zones;
    private int[,] weights;
    private Dictionary<DecorationCategory, List<string>> spritesByCategory;
    private Dictionary<string, int> footprintBySpriteName;
    private Dictionary<DecorationCategory, int> defaultFootprintByCategory;

    public IReadOnlyList<ZoneBounds> Zones => zones;

    public int GetWeight(DecorationCategory category, DecorationZone zone)
    {
        return weights[(int)category, (int)zone];
    }

    public IReadOnlyList<string> GetSpritesIn(DecorationCategory category)
    {
        if (spritesByCategory.TryGetValue(category, out var list)) return list;
        return new List<string>(0);
    }

    public int GetFootprintWidth(string spritePath, DecorationCategory category)
    {
        if (!string.IsNullOrEmpty(spritePath) && footprintBySpriteName.TryGetValue(spritePath, out int w)) return w;
        if (defaultFootprintByCategory.TryGetValue(category, out int dw)) return dw;
        return 1;
    }

    public DecorationZone? GetZoneAt(int x)
    {
        for (int i = 0; i < zones.Count; i++)
            if (zones[i].Contains(x)) return zones[i].Zone;
        return null;
    }

    // Default profile aligned with TASK-042 imported assets + SURFACE_DECORATION_RULES.
    public static SurfaceDecorationProfile CreateDefault()
    {
        var p = new SurfaceDecorationProfile
        {
            SurfaceWidth = 70,
            SurfaceHeight = 10,
            EntranceCenterX = 34,
        };

        p.zones = new List<ZoneBounds>
        {
            new ZoneBounds { Zone = DecorationZone.A_LeftNature,     StartX = 0,  EndX = 14 },
            new ZoneBounds { Zone = DecorationZone.B_LeftVillage,    StartX = 14, EndX = 27 },
            new ZoneBounds { Zone = DecorationZone.C_CenterEntrance, StartX = 27, EndX = 41 },
            new ZoneBounds { Zone = DecorationZone.D_RightVillage,   StartX = 41, EndX = 55 },
            new ZoneBounds { Zone = DecorationZone.E_RightLandmark,  StartX = 55, EndX = 70 },
        };

        // Weight matrix [category, zone]: 0=forbidden, 1=low, 2=medium, 3=high
        // Source: SURFACE_DECORATION_RULES § 四
        p.weights = new int[6, 5];
        // Background row stays 0 (not in random spawn pool; handled as bottom layer).
        // Entrance: only Zone C (spawner picks exactly 1)
        p.weights[(int)DecorationCategory.Entrance,      (int)DecorationZone.C_CenterEntrance] = 3;
        // SurfaceObject
        p.weights[(int)DecorationCategory.SurfaceObject, (int)DecorationZone.A_LeftNature]     = 3;
        p.weights[(int)DecorationCategory.SurfaceObject, (int)DecorationZone.B_LeftVillage]    = 1;
        p.weights[(int)DecorationCategory.SurfaceObject, (int)DecorationZone.C_CenterEntrance] = 2;
        p.weights[(int)DecorationCategory.SurfaceObject, (int)DecorationZone.D_RightVillage]   = 1;
        p.weights[(int)DecorationCategory.SurfaceObject, (int)DecorationZone.E_RightLandmark]  = 3;
        // Building
        p.weights[(int)DecorationCategory.Building,      (int)DecorationZone.A_LeftNature]     = 1;
        p.weights[(int)DecorationCategory.Building,      (int)DecorationZone.B_LeftVillage]    = 3;
        p.weights[(int)DecorationCategory.Building,      (int)DecorationZone.C_CenterEntrance] = 0;
        p.weights[(int)DecorationCategory.Building,      (int)DecorationZone.D_RightVillage]   = 3;
        p.weights[(int)DecorationCategory.Building,      (int)DecorationZone.E_RightLandmark]  = 2;
        // Prop
        p.weights[(int)DecorationCategory.Prop,          (int)DecorationZone.A_LeftNature]     = 1;
        p.weights[(int)DecorationCategory.Prop,          (int)DecorationZone.B_LeftVillage]    = 2;
        p.weights[(int)DecorationCategory.Prop,          (int)DecorationZone.C_CenterEntrance] = 3;
        p.weights[(int)DecorationCategory.Prop,          (int)DecorationZone.D_RightVillage]   = 2;
        p.weights[(int)DecorationCategory.Prop,          (int)DecorationZone.E_RightLandmark]  = 1;
        // Vegetation
        p.weights[(int)DecorationCategory.Vegetation,    (int)DecorationZone.A_LeftNature]     = 3;
        p.weights[(int)DecorationCategory.Vegetation,    (int)DecorationZone.B_LeftVillage]    = 2;
        p.weights[(int)DecorationCategory.Vegetation,    (int)DecorationZone.C_CenterEntrance] = 3;
        p.weights[(int)DecorationCategory.Vegetation,    (int)DecorationZone.D_RightVillage]   = 2;
        p.weights[(int)DecorationCategory.Vegetation,    (int)DecorationZone.E_RightLandmark]  = 2;

        p.spritesByCategory = new Dictionary<DecorationCategory, List<string>>
        {
            [DecorationCategory.Background] = new List<string>
            {
                "Assets/Art/Backgrounds/bg_overworld_00.png",
            },
            [DecorationCategory.Entrance] = new List<string>
            {
                "Assets/Art/Entrances/entrance_00.png",
                "Assets/Art/Entrances/entrance_01.png",
                "Assets/Art/Entrances/entrance_02.png",
                "Assets/Art/Entrances/entrance_03.png",
                "Assets/Art/Entrances/entrance_04.png",
            },
            [DecorationCategory.SurfaceObject] = new List<string>
            {
                "Assets/Art/SurfaceObjects/surface_tree_a_idle_00.png",
                "Assets/Art/SurfaceObjects/surface_tree_b_00.png",
                "Assets/Art/SurfaceObjects/surface_tree_c_00.png",
                "Assets/Art/SurfaceObjects/surface_watchtower_00.png",
            },
            [DecorationCategory.Building] = new List<string>
            {
                "Assets/Art/Buildings/building_00.png",
                "Assets/Art/Buildings/building_01.png",
                "Assets/Art/Buildings/building_02.png",
            },
            [DecorationCategory.Prop] = new List<string>
            {
                "Assets/Art/Props/prop_00.png", "Assets/Art/Props/prop_01.png",
                "Assets/Art/Props/prop_02.png", "Assets/Art/Props/prop_03.png",
                "Assets/Art/Props/prop_04.png", "Assets/Art/Props/prop_05.png",
                "Assets/Art/Props/prop_06.png", "Assets/Art/Props/prop_07.png",
                "Assets/Art/Props/prop_08.png", "Assets/Art/Props/prop_09.png",
                "Assets/Art/Props/prop_10.png",
            },
            [DecorationCategory.Vegetation] = new List<string>
            {
                "Assets/Art/Vegetation/veg_00.png", "Assets/Art/Vegetation/veg_01.png",
                "Assets/Art/Vegetation/veg_02.png", "Assets/Art/Vegetation/veg_03.png",
                "Assets/Art/Vegetation/veg_04.png",
            },
        };

        // Category default footprints (grid cells). Per SURFACE_DECORATION_RULES § 六.
        p.defaultFootprintByCategory = new Dictionary<DecorationCategory, int>
        {
            [DecorationCategory.Background]    = 70,
            [DecorationCategory.Entrance]      = 8,
            [DecorationCategory.SurfaceObject] = 6,
            [DecorationCategory.Building]      = 5,
            [DecorationCategory.Prop]          = 1,
            [DecorationCategory.Vegetation]    = 1,
        };

        // Per-sprite footprint overrides: empty for now; user can fill after visual review.
        p.footprintBySpriteName = new Dictionary<string, int>();

        return p;
    }
}
