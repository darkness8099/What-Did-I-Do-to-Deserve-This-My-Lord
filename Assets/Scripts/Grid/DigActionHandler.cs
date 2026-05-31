using UnityEngine;

public class DigActionHandler : MonoBehaviour
{
    private GridManager gridManager;
    private GridRenderer gridRenderer;
    private MonsterManager monsterManager;
    private MonsterRenderer monsterRenderer;

    private void Awake()
    {
        gridManager = GetComponent<GridManager>() ?? FindObjectOfType<GridManager>();
        gridRenderer = GetComponent<GridRenderer>() ?? FindObjectOfType<GridRenderer>();
        monsterManager = GetComponent<MonsterManager>() ?? FindObjectOfType<MonsterManager>();
        monsterRenderer = GetComponent<MonsterRenderer>() ?? FindObjectOfType<MonsterRenderer>();
    }

    public void HandlePrimaryClick(int x, int y)
    {
        if (gridManager == null || gridRenderer == null || monsterManager == null || monsterRenderer == null)
        {
            Debug.LogError("[DigActionHandler] Required manager missing.");
            return;
        }

        if (!gridManager.IsInside(x, y))
        {
            Debug.Log($"[DigActionHandler] Click outside map: ({x},{y})");
            return;
        }

        CellType cell = gridManager.GetCellType(x, y);
        if (cell == CellType.Soil)
        {
            DigSoilCell(x, y);
            return;
        }

        if (cell == CellType.Empty)
            Debug.Log($"[DigActionHandler] Empty tile clicked at ({x},{y}), no action.");
    }

    private void DigSoilCell(int x, int y)
    {
        // Snapshot tile attribute while cell is still Soil (valid resource container)
        TileAttributeData attr = gridManager.GetTileAttribute(x, y);

        if (!gridManager.DigCell(x, y))
            return;

        gridRenderer.RefreshCell(x, y);
        // Cell is Empty now → cannot hold tile attribute resources

        if (attr.CanSpawnMonster())
        {
            MonsterArchetype archetype = ResolveArchetypeForElement(attr.ElementType);
            if (archetype != null && monsterManager.PlaceMonster(x, y, archetype))
            {
                MonsterData data = monsterManager.GetMonster(x, y);
                data.AbsorbFromTile(ref attr);
                monsterRenderer.CreateMonsterView(x, y, data);
                Debug.Log($"[Resource] Dig({x},{y}): tile→{data.DisplayName} N={data.CurrentNutrient} M={data.CurrentMagic}; tile remaining N={attr.Nutrient} M={attr.Magic}");
            }
            attr.ElementType = TileElementType.None;
        }

        // Leftover resources after spawn (or all resources if no monster spawned) scatter to surrounding Soil
        if (attr.HasResource())
        {
            ResourceFlow.ScatterDigLeftoverResources(new Vector2Int(x, y), attr.Nutrient, attr.Magic, gridManager, $"dig({x},{y})");
            RefreshNearbyCells(x, y, 3);
        }
    }

    private MonsterArchetype ResolveArchetypeForElement(TileElementType element)
    {
        switch (element)
        {
            case TileElementType.Slime: return MonsterArchetype.Slime;
            default: return null;
        }
    }

    private void RefreshNearbyCells(int centerX, int centerY, int radius)
    {
        for (int x = centerX - radius; x <= centerX + radius; x++)
            for (int y = centerY - radius; y <= centerY + radius; y++)
                if (gridManager.IsInside(x, y))
                    gridRenderer.RefreshCell(x, y);
    }
}
