public enum TileElementType
{
    None,
    Slime
}

public struct TileAttributeData
{
    public int            MagicPower;
    public TileElementType ElementType;

    public TileAttributeData(int magicPower, TileElementType elementType)
    {
        MagicPower  = magicPower;
        ElementType = elementType;
    }

    public static TileAttributeData Default =>
        new TileAttributeData(0, TileElementType.None);

    public bool CanSpawnMonster()
    {
        return MagicPower > 0 && ElementType != TileElementType.None;
    }
}
