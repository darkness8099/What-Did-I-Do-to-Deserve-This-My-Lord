// One decoration placement entry produced by SurfaceDecorationSpawner.
// X is the LEFT edge of the footprint in grid cells; vertical position is decided by renderer.
public struct DecorationPlacementData
{
    public string              SpritePath;
    public DecorationCategory  Category;
    public DecorationZone      Zone;
    public int                 X;
    public int                 FootprintWidth;
    public int                 SortingOrder;

    public int RightX  => X + FootprintWidth;
    public int CenterX => X + FootprintWidth / 2;
}

// Sorting-order constants per SURFACE_DECORATION_RULES § 五.
public static class DecorationSortingLayer
{
    public const int BG_Base       = -100;
    public const int BG_BackDeco   = -80;
    public const int BG_MidDeco    = -60;
    public const int BG_FrontDeco  = -40;
    public const int BG_TopDeco    = -30;

    public static int ResolveByCategory(DecorationCategory category)
    {
        switch (category)
        {
            case DecorationCategory.Background:    return BG_Base;
            case DecorationCategory.SurfaceObject: return BG_MidDeco;
            case DecorationCategory.Building:      return BG_MidDeco;
            case DecorationCategory.Entrance:      return BG_MidDeco;
            case DecorationCategory.Prop:          return BG_FrontDeco;
            case DecorationCategory.Vegetation:    return BG_TopDeco;
            default:                                return BG_FrontDeco;
        }
    }
}
