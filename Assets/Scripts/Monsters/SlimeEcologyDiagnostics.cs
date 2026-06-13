using System.IO;
using UnityEngine;

// TEMPORARY diagnostics: writes slime/moss ecology events to a local log file (one record per line)
// instead of spamming the Unity Console. Toggle via EcologyTickDriver (Enable/MaxLines) or the statics below.
// File: Application.persistentDataPath/slime_ecology_diagnostics.log
public static class SlimeEcologyDiagnostics
{
    public static bool Enabled = true;     // EnableSlimeEcologyDiagnostics
    public static int  MaxLines = 300;     // MaxDiagnosticLines

    private static string _path;
    private static int _lines;
    private static bool _ready;

    public static string FilePath => _path;

    public static void Configure(bool enabled, int maxLines)
    {
        Enabled  = enabled;
        MaxLines = Mathf.Max(1, maxLines);
    }

    // Start a fresh file (call once per play session).
    public static void Begin()
    {
        _ready = false;
        _lines = 0;
        if (!Enabled) return;
        try
        {
            _path = Path.Combine(Application.persistentDataPath, "slime_ecology_diagnostics.log");
            File.WriteAllText(_path, $"# slime_ecology_diagnostics — {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
            _ready = true;
            Debug.Log($"[SlimeDiag] logging to: {_path}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[SlimeDiag] could not open log file: " + e.Message);
            _ready = false;
        }
    }

    private static void Write(string line)
    {
        if (!Enabled || !_ready || _lines >= MaxLines) return;
        try
        {
            File.AppendAllText(_path, line + "\n");
            _lines++;
            if (_lines == MaxLines)
                File.AppendAllText(_path, "# --- MaxDiagnosticLines reached; further records suppressed ---\n");
        }
        catch { /* swallow: diagnostics must never break gameplay */ }
    }

    // ===== typed records (one line each) =====

    public static void Global(float t, long totalSoilNutrient, int nutrientCells, int soilCells)
        => Write($"[GLOBAL] time={t:F1} totalSoilNutrient={totalSoilNutrient} nutrientCells={nutrientCells} soilCells={soilCells}");

    public static void BudSpawn(float t, Vector2Int p, int slimeHp, int slimeNutrient, int area5x5Nutrient, int area5x5CellsWithNutrient)
        => Write($"[BUD_SPAWN] time={t:F1} pos=({p.x},{p.y}) slimeHp={slimeHp} slimeNutrient={slimeNutrient} area5x5Nutrient={area5x5Nutrient} area5x5CellsWithNutrient={area5x5CellsWithNutrient}");

    public static void BudTick(float t, Vector2Int p, int hp, int nutrient, int absorbed, int hpDelta, string reason)
        => Write($"[BUD_TICK] time={t:F1} pos=({p.x},{p.y}) hp={hp} nutrient={nutrient} absorbed={absorbed} hpDelta={hpDelta} reason={reason}");

    public static void BudResult(float t, Vector2Int p, string result, int hp, int nutrient, int area5x5NutrientLeft)
        => Write($"[BUD_RESULT] time={t:F1} pos=({p.x},{p.y}) result={result} hp={hp} nutrient={nutrient} area5x5NutrientLeft={area5x5NutrientLeft}");

    public static void FlowerResult(float t, Vector2Int p, int absorbedTotal, int plannedSpawn, int actualSpawn, string failReason, string spawnDelays)
        => Write($"[FLOWER_RESULT] time={t:F1} pos=({p.x},{p.y}) absorbedTotal={absorbedTotal} plannedSpawn={plannedSpawn} actualSpawn={actualSpawn} failReason={failReason} spawnDelays=[{spawnDelays}]");

    // ===== helpers =====

    // Sum nutrient and count nutrient-bearing cells in the 5x5 box around `p` (Soil tiles only).
    public static void Area5x5(GridManager grid, Vector2Int p, out int totalNutrient, out int cellsWithNutrient)
    {
        totalNutrient = 0;
        cellsWithNutrient = 0;
        if (grid == null) return;
        for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
            {
                int cx = p.x + dx, cy = p.y + dy;
                if (!grid.IsInside(cx, cy)) continue;
                if (grid.GetCellType(cx, cy) != CellType.Soil) continue;
                int n = grid.GetTileAttribute(cx, cy).Nutrient;
                if (n > 0) { totalNutrient += n; cellsWithNutrient++; }
            }
    }
}
