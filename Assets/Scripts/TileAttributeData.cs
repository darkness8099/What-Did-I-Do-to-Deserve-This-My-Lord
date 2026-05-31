using UnityEngine;

// Tile element flavor — declares what kind of monster this tile is biased to spawn.
// Resource axes (Nutrient/Magic) live on the same struct but are independent quantities.
public enum TileElementType
{
    None,
    Slime,
}

// Per-cell ecology container. Carries two resource axes plus a spawn-bias element.
// Only valid on cells of CellType.Soil — GridManager enforces this.
public struct TileAttributeData
{
    public int             Nutrient;
    public int             Magic;
    public TileElementType ElementType;

    public TileAttributeData(int nutrient, int magic, TileElementType elementType)
    {
        Nutrient    = Mathf.Max(0, nutrient);
        Magic       = Mathf.Max(0, magic);
        ElementType = elementType;
    }

    public static TileAttributeData Default =>
        new TileAttributeData(0, 0, TileElementType.None);

    public bool HasResource()     => Nutrient > 0 || Magic > 0;
    public bool CanSpawnMonster() => HasResource() && ElementType != TileElementType.None;

    // Withdraw up to `request`; returns the actual amount removed (clamped to current stock).
    public int WithdrawNutrient(int request)
    {
        int take = Mathf.Clamp(request, 0, Nutrient);
        Nutrient -= take;
        return take;
    }

    public int WithdrawMagic(int request)
    {
        int take = Mathf.Clamp(request, 0, Magic);
        Magic -= take;
        return take;
    }

    public void DepositNutrient(int amount)
    {
        if (amount <= 0) return;
        Nutrient += amount;
    }

    public void DepositMagic(int amount)
    {
        if (amount <= 0) return;
        Magic += amount;
    }
}
