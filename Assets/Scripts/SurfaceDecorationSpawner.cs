using System.Collections.Generic;
using UnityEngine;

// Generates a placement draft according to SurfaceDecorationProfile + SURFACE_DECORATION_RULES § 七.
// OUTPUT-ONLY: produces List<DecorationPlacementData>; does NOT instantiate GameObjects.
// Instantiation is BackgroundLayerRenderer's job (TASK-046).
public class SurfaceDecorationSpawner : MonoBehaviour
{
    private const int MaxRetryPerPlacement = 20;

    [Header("Random Seed")]
    [Tooltip("0 = use Environment.TickCount (nondeterministic). Non-zero = deterministic.")]
    [SerializeField] private int randomSeed = 0;

    [Header("Generation Targets (per SURFACE_DECORATION_RULES § 七)")]
    [SerializeField] private int zoneBBuildingsMin = 1;
    [SerializeField] private int zoneBBuildingsMax = 2;
    [SerializeField] private int zoneDBuildingsMin = 1;
    [SerializeField] private int zoneDBuildingsMax = 2;
    [SerializeField] private int zoneCPropsAroundEntranceMin = 3;
    [SerializeField] private int zoneCPropsAroundEntranceMax = 6;
    [SerializeField] private int sprinkleMin = 10;
    [SerializeField] private int sprinkleMax = 20;

    private SurfaceDecorationProfile profile;
    private List<DecorationPlacementData> currentDraft;

    public int RandomSeed { get => randomSeed; set => randomSeed = value; }
    public IReadOnlyList<DecorationPlacementData> CurrentDraft => currentDraft;
    public SurfaceDecorationProfile Profile => profile;

    private void Awake()
    {
        EnsureInitialized();
    }

    public void ClearDraft()
    {
        EnsureInitialized();
        if (currentDraft != null) currentDraft.Clear();
    }

    public List<DecorationPlacementData> RegenerateDraft()
    {
        ClearDraft();
        return GenerateDraft();
    }

    public List<DecorationPlacementData> GenerateDraft()
    {
        EnsureInitialized();
        if (currentDraft.Count > 0) currentDraft.Clear();

        int seed = randomSeed != 0 ? randomSeed : System.Environment.TickCount;
        var rng = new System.Random(seed);

        // Step 1: fixed entrance in Zone C, near EntranceCenterX
        TryPlaceEntrance(rng);

        // Step 2: Zone A — 1 large natural landmark (SurfaceObject)
        PlaceInZone(rng, DecorationZone.A_LeftNature, DecorationCategory.SurfaceObject, 1);

        // Step 3: Zone E — 1 closing landmark; pick SurfaceObject or Building 50/50
        PlaceInZone(rng, DecorationZone.E_RightLandmark,
            rng.Next(2) == 0 ? DecorationCategory.SurfaceObject : DecorationCategory.Building, 1);

        // Step 4: Zone B / D — 1~2 buildings each
        PlaceInZone(rng, DecorationZone.B_LeftVillage, DecorationCategory.Building,
            rng.Next(zoneBBuildingsMin, zoneBBuildingsMax + 1));
        PlaceInZone(rng, DecorationZone.D_RightVillage, DecorationCategory.Building,
            rng.Next(zoneDBuildingsMin, zoneDBuildingsMax + 1));

        // Step 5: Zone C — 3~6 Props/Vegetation around entrance
        int zoneCCount = rng.Next(zoneCPropsAroundEntranceMin, zoneCPropsAroundEntranceMax + 1);
        for (int i = 0; i < zoneCCount; i++)
        {
            DecorationCategory cat = rng.Next(2) == 0 ? DecorationCategory.Prop : DecorationCategory.Vegetation;
            TryPlaceOnce(rng, DecorationZone.C_CenterEntrance, cat);
        }

        // Step 6: full-map sprinkle 10~20 Props/Vegetation, zone weighted
        int sprinkleCount = rng.Next(sprinkleMin, sprinkleMax + 1);
        for (int i = 0; i < sprinkleCount; i++)
        {
            DecorationCategory cat = rng.Next(2) == 0 ? DecorationCategory.Prop : DecorationCategory.Vegetation;
            DecorationZone? zone = PickZoneByWeight(rng, cat);
            if (zone.HasValue) TryPlaceOnce(rng, zone.Value, cat);
        }

        Debug.Log($"[SurfaceDecorationSpawner] Draft generated: {currentDraft.Count} placements (seed={seed}).");
        return currentDraft;
    }

    private void TryPlaceEntrance(System.Random rng)
    {
        var sprites = profile.GetSpritesIn(DecorationCategory.Entrance);
        if (sprites.Count == 0) { Debug.LogWarning("[Spawner] No Entrance sprites in profile."); return; }

        string spritePath = sprites[rng.Next(sprites.Count)];
        int footprint = profile.GetFootprintWidth(spritePath, DecorationCategory.Entrance);
        int x = profile.EntranceCenterX - footprint / 2;
        var bounds = profile.Zones[(int)DecorationZone.C_CenterEntrance];
        x = Mathf.Clamp(x, bounds.StartX, bounds.EndX - footprint);

        currentDraft.Add(new DecorationPlacementData
        {
            SpritePath     = spritePath,
            Category       = DecorationCategory.Entrance,
            Zone           = DecorationZone.C_CenterEntrance,
            X              = x,
            FootprintWidth = footprint,
            SortingOrder   = DecorationSortingLayer.ResolveByCategory(DecorationCategory.Entrance),
        });
    }

    private void PlaceInZone(System.Random rng, DecorationZone zone, DecorationCategory category, int count)
    {
        for (int i = 0; i < count; i++) TryPlaceOnce(rng, zone, category);
    }

    private void TryPlaceOnce(System.Random rng, DecorationZone zone, DecorationCategory category)
    {
        var sprites = profile.GetSpritesIn(category);
        if (sprites.Count == 0) return;

        var bounds = profile.Zones[(int)zone];

        for (int attempt = 0; attempt < MaxRetryPerPlacement; attempt++)
        {
            string spritePath = sprites[rng.Next(sprites.Count)];
            int fp = profile.GetFootprintWidth(spritePath, category);
            int maxStartX = bounds.EndX - fp;
            if (maxStartX < bounds.StartX) continue;

            int x = rng.Next(bounds.StartX, maxStartX + 1);
            if (OverlapsExistingSameLayer(x, fp, category)) continue;

            currentDraft.Add(new DecorationPlacementData
            {
                SpritePath     = spritePath,
                Category       = category,
                Zone           = zone,
                X              = x,
                FootprintWidth = fp,
                SortingOrder   = DecorationSortingLayer.ResolveByCategory(category),
            });
            return;
        }
    }

    // Only enforce no-overlap within the same sorting layer; cross-layer overlap is fine
    // (e.g., a small prop in front of a tree).
    private bool OverlapsExistingSameLayer(int x, int width, DecorationCategory category)
    {
        int layer = DecorationSortingLayer.ResolveByCategory(category);
        int right = x + width;
        for (int i = 0; i < currentDraft.Count; i++)
        {
            var d = currentDraft[i];
            if (d.SortingOrder != layer) continue;
            if (x < d.RightX && right > d.X) return true;
        }
        return false;
    }

    private DecorationZone? PickZoneByWeight(System.Random rng, DecorationCategory category)
    {
        int total = 0;
        for (int z = 0; z < 5; z++) total += profile.GetWeight(category, (DecorationZone)z);
        if (total <= 0) return null;

        int pick = rng.Next(total);
        int acc = 0;
        for (int z = 0; z < 5; z++)
        {
            acc += profile.GetWeight(category, (DecorationZone)z);
            if (pick < acc) return (DecorationZone)z;
        }
        return null;
    }

    private void EnsureInitialized()
    {
        if (profile == null) profile = SurfaceDecorationProfile.CreateDefault();
        if (currentDraft == null) currentDraft = new List<DecorationPlacementData>();
    }
}
