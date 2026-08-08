using UnityEngine;

public class DemonLordManager : MonoBehaviour
{
    private LevelConfig levelConfig;
    private Vector2Int position;

    public bool IsCaptured { get; private set; }
    public bool IsPlaced { get; private set; } = true;
    public bool IsRepositioning { get; private set; }
    public bool IsWaitingForPlacement => IsRepositioning;
    public int CaptorHeroId { get; private set; } = -1;

    private void Awake()
    {
        levelConfig = GetComponent<LevelConfig>() ?? FindObjectOfType<LevelConfig>();
        if (levelConfig == null)
        {
            Debug.LogError("[DemonLordManager] LevelConfig not found in scene.");
            return;
        }

        position = levelConfig.DemonLordStartPosition;
        Debug.Log($"[DemonLordManager] DemonLord initialized at ({position.x},{position.y}).");
    }

    public Vector2Int GetPosition()
    {
        return position;
    }

    public void SetPosition(Vector2Int newPosition)
    {
        if (IsCaptured) return;
        position = newPosition;
    }

    public void RequestReposition()
    {
        if (IsCaptured || IsRepositioning) return;

        IsPlaced = false;
        IsRepositioning = true;
        Debug.Log("[DemonLordManager] DemonLord picked up. Select an Empty cell to place.");
    }

    public bool TryPlaceAt(Vector2Int newPosition, GridManager gridManager)
    {
        if (!IsRepositioning || IsCaptured) return false;
        if (gridManager == null || !gridManager.IsInside(newPosition.x, newPosition.y)) return false;
        if (gridManager.GetCellType(newPosition.x, newPosition.y) != CellType.Empty) return false;

        position = newPosition;
        IsPlaced = true;
        IsRepositioning = false;
        Debug.Log($"[DemonLordManager] DemonLord placed at ({position.x},{position.y}).");
        return true;
    }

    public bool Capture(int heroId)
    {
        if (IsCaptured) return false;

        IsCaptured = true;
        IsPlaced = false;
        IsRepositioning = false;
        CaptorHeroId = heroId;
        Debug.Log($"[DemonLordManager] DemonLord captured by Hero {heroId}.");
        return true;
    }

    public bool UpdateCapturedPosition(int heroId, Vector2Int newPosition)
    {
        if (!IsCaptured || CaptorHeroId != heroId) return false;
        position = newPosition;
        return true;
    }

    public bool ReleaseCaptureAt(int heroId, Vector2Int dropPosition)
    {
        if (!IsCaptured || CaptorHeroId != heroId) return false;

        position = dropPosition;
        IsCaptured = false;
        IsPlaced = true;
        IsRepositioning = false;
        CaptorHeroId = -1;
        Debug.Log($"[DemonLordManager] DemonLord dropped at ({position.x},{position.y}) after Hero {heroId} was defeated.");
        return true;
    }
}
