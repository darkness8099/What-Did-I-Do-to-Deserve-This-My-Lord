using System.Collections.Generic;
using UnityEngine;

// Generates an editor-side placement draft according to SurfaceDecorationProfile rules.
// OUTPUT-ONLY: produces List<DecorationPlacementData>; does NOT instantiate GameObjects.
// Instantiation remains BackgroundLayerRenderer's job.
public class SurfaceDecorationSpawner : MonoBehaviour
{
    private const int MaxRetryPerPlacement = 20;
    private const int SmallGapLength = 4;
    private const int MajorGapLength = 8;

    [Header("Random Seed")]
    [Tooltip("0 = use Environment.TickCount (nondeterministic). Non-zero = deterministic.")]
    [SerializeField] private int randomSeed = 0;

    [Header("Generation Targets")]
    [SerializeField] private int zoneBBuildingsMin = 1;
    [SerializeField] private int zoneBBuildingsMax = 2;
    [SerializeField] private int zoneDBuildingsMin = 1;
    [SerializeField] private int zoneDBuildingsMax = 2;
    [SerializeField] private int zoneCPropsAroundEntranceMin = 8;
    [SerializeField] private int zoneCPropsAroundEntranceMax = 12;
    [SerializeField] private int sprinkleMin = 25;
    [SerializeField] private int sprinkleMax = 40;

    [Header("Attached Decoration")]
    [SerializeField] private int attachedDecorMin = 2;
    [SerializeField] private int attachedDecorMax = 5;
    [SerializeField] private int attachedDecorOffsetMin = 1;
    [SerializeField] private int attachedDecorOffsetMax = 3;

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

        DecorationPlacementData? entrance = TryPlaceEntrance(rng);
        if (entrance.HasValue)
            PlaceAttachedDecorations(rng, entrance.Value);

        PlaceInZone(rng, DecorationZone.A_LeftNature, DecorationCategory.SurfaceObject, 1, true);

        PlaceInZone(
            rng,
            DecorationZone.E_RightLandmark,
            rng.Next(2) == 0 ? DecorationCategory.SurfaceObject : DecorationCategory.Building,
            1,
            true);

        PlaceInZone(
            rng,
            DecorationZone.B_LeftVillage,
            DecorationCategory.Building,
            rng.Next(zoneBBuildingsMin, zoneBBuildingsMax + 1),
            true);

        PlaceInZone(
            rng,
            DecorationZone.D_RightVillage,
            DecorationCategory.Building,
            rng.Next(zoneDBuildingsMin, zoneDBuildingsMax + 1),
            true);

        int zoneCCount = rng.Next(zoneCPropsAroundEntranceMin, zoneCPropsAroundEntranceMax + 1);
        for (int i = 0; i < zoneCCount; i++)
            TryPlaceOnce(rng, DecorationZone.C_CenterEntrance, PickSmallCategory(rng));

        int sprinkleCount = rng.Next(sprinkleMin, sprinkleMax + 1);
        for (int i = 0; i < sprinkleCount; i++)
        {
            DecorationCategory category = PickSmallCategory(rng);
            DecorationZone? zone = PickZoneByWeight(rng, category);
            if (zone.HasValue) TryPlaceOnce(rng, zone.Value, category);
        }

        FillSparseSurface(rng);

        Debug.Log($"[SurfaceDecorationSpawner] Draft generated: {currentDraft.Count} placements (seed={seed}).");
        return currentDraft;
    }

    private DecorationPlacementData? TryPlaceEntrance(System.Random rng)
    {
        IReadOnlyList<string> sprites = profile.GetSpritesIn(DecorationCategory.Entrance);
        if (sprites.Count == 0)
        {
            Debug.LogWarning("[SurfaceDecorationSpawner] No entrance sprites in profile.");
            return null;
        }

        string spritePath = sprites[rng.Next(sprites.Count)];
        int footprint = profile.GetFootprintWidth(spritePath, DecorationCategory.Entrance);
        ZoneBounds bounds = profile.Zones[(int)DecorationZone.C_CenterEntrance];
        int x = Mathf.Clamp(profile.EntranceCenterX - footprint / 2, bounds.StartX, bounds.EndX - footprint);

        DecorationPlacementData placement = new DecorationPlacementData
        {
            SpritePath = spritePath,
            Category = DecorationCategory.Entrance,
            Zone = DecorationZone.C_CenterEntrance,
            X = x,
            FootprintWidth = footprint,
            SortingOrder = DecorationSortingLayer.ResolveByCategory(DecorationCategory.Entrance),
        };

        currentDraft.Add(placement);
        return placement;
    }

    private void PlaceInZone(System.Random rng, DecorationZone zone, DecorationCategory category, int count, bool spawnAttachments)
    {
        for (int i = 0; i < count; i++)
        {
            DecorationPlacementData placement;
            if (TryPlaceOnce(rng, zone, category, out placement) && spawnAttachments)
                PlaceAttachedDecorations(rng, placement);
        }
    }

    private void TryPlaceOnce(System.Random rng, DecorationZone zone, DecorationCategory category)
    {
        DecorationPlacementData _;
        TryPlaceOnce(rng, zone, category, out _);
    }

    private bool TryPlaceOnce(System.Random rng, DecorationZone zone, DecorationCategory category, out DecorationPlacementData placement)
    {
        placement = default(DecorationPlacementData);

        IReadOnlyList<string> sprites = profile.GetSpritesIn(category);
        if (sprites.Count == 0) return false;

        ZoneBounds bounds = profile.Zones[(int)zone];
        for (int attempt = 0; attempt < MaxRetryPerPlacement; attempt++)
        {
            string spritePath = sprites[rng.Next(sprites.Count)];
            int footprint = profile.GetFootprintWidth(spritePath, category);
            int maxStartX = bounds.EndX - footprint;
            if (maxStartX < bounds.StartX) continue;

            int x = rng.Next(bounds.StartX, maxStartX + 1);
            if (OverlapsExistingSameLayer(x, footprint, category)) continue;

            placement = new DecorationPlacementData
            {
                SpritePath = spritePath,
                Category = category,
                Zone = zone,
                X = x,
                FootprintWidth = footprint,
                SortingOrder = DecorationSortingLayer.ResolveByCategory(category),
            };

            currentDraft.Add(placement);
            return true;
        }

        return false;
    }

    private void PlaceAttachedDecorations(System.Random rng, DecorationPlacementData anchor)
    {
        if (!IsMajorCategory(anchor.Category)) return;

        int count = rng.Next(attachedDecorMin, attachedDecorMax + 1);
        for (int i = 0; i < count; i++)
        {
            bool placeRight = rng.Next(2) == 0;
            int offset = rng.Next(attachedDecorOffsetMin, attachedDecorOffsetMax + 1);
            int centerX = placeRight ? anchor.RightX + offset : anchor.X - offset;
            TryPlaceNearX(rng, anchor.Zone, PickSmallCategory(rng), centerX);
        }
    }

    private void FillSparseSurface(System.Random rng)
    {
        for (int x = 0; x <= profile.SurfaceWidth - SmallGapLength; x++)
        {
            if (HasAnyDecorationInRange(x, x + SmallGapLength)) continue;

            DecorationZone? zone = profile.GetZoneAt(x + SmallGapLength / 2);
            if (!zone.HasValue) continue;

            TryPlaceNearX(rng, zone.Value, PickSmallCategory(rng), x + SmallGapLength / 2);
        }

        for (int x = 0; x <= profile.SurfaceWidth - MajorGapLength; x++)
        {
            if (HasMajorDecorationInRange(x, x + MajorGapLength)) continue;

            int centerX = x + MajorGapLength / 2;
            DecorationZone? zone = profile.GetZoneAt(centerX);
            if (!zone.HasValue) continue;

            DecorationCategory category = rng.Next(2) == 0 ? DecorationCategory.SurfaceObject : DecorationCategory.Building;
            if (profile.GetWeight(category, zone.Value) <= 0) continue;

            DecorationPlacementData placement;
            if (TryPlaceNearX(rng, zone.Value, category, centerX, out placement))
                PlaceAttachedDecorations(rng, placement);
        }
    }

    private bool TryPlaceNearX(System.Random rng, DecorationZone zone, DecorationCategory category, int centerX)
    {
        DecorationPlacementData _;
        return TryPlaceNearX(rng, zone, category, centerX, out _);
    }

    private bool TryPlaceNearX(System.Random rng, DecorationZone zone, DecorationCategory category, int centerX, out DecorationPlacementData placement)
    {
        placement = default(DecorationPlacementData);

        IReadOnlyList<string> sprites = profile.GetSpritesIn(category);
        if (sprites.Count == 0) return false;

        ZoneBounds bounds = profile.Zones[(int)zone];
        for (int attempt = 0; attempt < MaxRetryPerPlacement; attempt++)
        {
            string spritePath = sprites[rng.Next(sprites.Count)];
            int footprint = profile.GetFootprintWidth(spritePath, category);
            int idealX = centerX - footprint / 2;
            int minX = Mathf.Max(bounds.StartX, idealX - 2);
            int maxX = Mathf.Min(bounds.EndX - footprint, idealX + 2);
            if (maxX < minX) continue;

            int x = rng.Next(minX, maxX + 1);
            if (OverlapsExistingSameLayer(x, footprint, category)) continue;

            placement = new DecorationPlacementData
            {
                SpritePath = spritePath,
                Category = category,
                Zone = zone,
                X = x,
                FootprintWidth = footprint,
                SortingOrder = DecorationSortingLayer.ResolveByCategory(category),
            };

            currentDraft.Add(placement);
            return true;
        }

        return false;
    }

    private bool OverlapsExistingSameLayer(int x, int width, DecorationCategory category)
    {
        int layer = DecorationSortingLayer.ResolveByCategory(category);
        int right = x + width;

        for (int i = 0; i < currentDraft.Count; i++)
        {
            DecorationPlacementData existing = currentDraft[i];
            if (existing.SortingOrder != layer) continue;
            if (x < existing.RightX && right > existing.X) return true;
        }

        return false;
    }

    private DecorationZone? PickZoneByWeight(System.Random rng, DecorationCategory category)
    {
        int total = 0;
        for (int z = 0; z < 5; z++)
            total += profile.GetWeight(category, (DecorationZone)z);

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

    private bool HasAnyDecorationInRange(int startX, int endXExclusive)
    {
        for (int i = 0; i < currentDraft.Count; i++)
        {
            DecorationPlacementData decoration = currentDraft[i];
            if (decoration.X < endXExclusive && decoration.RightX > startX)
                return true;
        }

        return false;
    }

    private bool HasMajorDecorationInRange(int startX, int endXExclusive)
    {
        for (int i = 0; i < currentDraft.Count; i++)
        {
            DecorationPlacementData decoration = currentDraft[i];
            if (!IsMajorCategory(decoration.Category)) continue;
            if (decoration.X < endXExclusive && decoration.RightX > startX)
                return true;
        }

        return false;
    }

    private static DecorationCategory PickSmallCategory(System.Random rng)
    {
        return rng.Next(2) == 0 ? DecorationCategory.Prop : DecorationCategory.Vegetation;
    }

    private static bool IsMajorCategory(DecorationCategory category)
    {
        return category == DecorationCategory.Entrance
            || category == DecorationCategory.SurfaceObject
            || category == DecorationCategory.Building;
    }

    private void EnsureInitialized()
    {
        if (profile == null) profile = SurfaceDecorationProfile.CreateDefault();
        if (currentDraft == null) currentDraft = new List<DecorationPlacementData>();
    }
}
