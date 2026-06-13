using System.Collections.Generic;
using UnityEngine;

// Ecology tick driver — advances moss/slime ecology on a single fixed interval (NOT per-frame per monster).
// Per tick, for each monster (snapshot), dispatch by stage, fire death animations, then reconcile views once.
public class EcologyTickDriver : MonoBehaviour
{
    [SerializeField] private float tickSeconds = 1.0f;

    [Header("Diagnostics (temporary)")]
    [SerializeField] private bool enableSlimeEcologyDiagnostics = true;
    [SerializeField] private int maxDiagnosticLines = 300;

    private const float GlobalLogSeconds = 5f;

    private MonsterManager monsters;
    private GridManager grid;
    private MonsterRenderer view;
    private float timer;
    private float globalTimer;
    private readonly List<MonsterData> scratch = new List<MonsterData>();

    private void Start()
    {
        monsters = GetComponent<MonsterManager>();
        if (monsters == null) monsters = FindObjectOfType<MonsterManager>();
        grid = GetComponent<GridManager>();
        if (grid == null) grid = FindObjectOfType<GridManager>();
        view = GetComponent<MonsterRenderer>();
        if (view == null) view = FindObjectOfType<MonsterRenderer>();

        if (monsters == null || grid == null)
        {
            Debug.LogError("[EcologyTickDriver] Missing MonsterManager/GridManager. Disabled.");
            enabled = false;
            return;
        }

        SlimeEcologyDiagnostics.Configure(enableSlimeEcologyDiagnostics, maxDiagnosticLines);
        SlimeEcologyDiagnostics.Begin();

        Debug.Log("[EcologyTickDriver] Initialized.");
    }

    private void Update()
    {
        globalTimer += Time.deltaTime;
        if (globalTimer >= GlobalLogSeconds)
        {
            globalTimer -= GlobalLogSeconds;
            LogGlobal();
        }

        timer += Time.deltaTime;
        if (timer < tickSeconds) return;
        timer -= tickSeconds;
        ProcessTick();
    }

    // [GLOBAL] every 5s: full-grid soil-nutrient snapshot (diagnostics only; gated by Enabled).
    private void LogGlobal()
    {
        if (!SlimeEcologyDiagnostics.Enabled || grid == null) return;
        GridData gd = grid.GetGridData();
        if (gd == null) return;

        long total = 0; int nutCells = 0, soilCells = 0;
        for (int x = 0; x < gd.Width; x++)
            for (int y = 0; y < gd.Height; y++)
            {
                if (gd.GetCell(x, y) != CellType.Soil) continue;
                soilCells++;
                int n = gd.GetTileAttribute(x, y).Nutrient;
                if (n > 0) { total += n; nutCells++; }
            }
        SlimeEcologyDiagnostics.Global(Time.time, total, nutCells, soilCells);
    }

    private void ProcessTick()
    {
        monsters.CollectAll(scratch);
        foreach (MonsterData m in scratch)
        {
            switch (m.Stage)
            {
                case SlimeLifecycleStage.Crawling:
                    TickCrawling(m);
                    break;

                case SlimeLifecycleStage.Bud:
                    if (MonsterLifecycleSystem.BudTick(m, monsters, grid) == StageTickOutcome.WitherFailed)
                        if (view != null) view.NotifyMonsterDied(m);
                    break;

                case SlimeLifecycleStage.Flower:
                    if (MonsterLifecycleSystem.FlowerTick(m, monsters, grid) == StageTickOutcome.Reproduced)
                        if (view != null) view.NotifyMonsterDied(m);
                    break;
            }
        }

        if (view != null) view.SyncViews(monsters);
    }

    private void TickCrawling(MonsterData m)
    {
        // Newborn startup delay: idle (no move / ecology / HP cost) until the delay elapses.
        if (m.IsSpawnDelayed()) return;

        Vector2Int cur;
        bool moved = MonsterMovementSystem.TryMoveStep(m, grid, out cur);
        if (moved) m.RegisterMove();

        // HP move cost on its own cadence (lose HpCostPerMove every HpCostCooldownMoves moves).
        int hpCd = Mathf.Max(1, m.Archetype.HpCostCooldownMoves);
        if (m.MovesSinceHpCost >= hpCd)
        {
            MonsterLifecycleSystem.ApplyMoveHpCost(m);
            m.ResetHpCostCounter();
        }

        // Ecology (absorb/release) only after accumulating enough MOVES — never churn in place, never every frame.
        int cooldown = Mathf.Max(1, m.Archetype.EcologyActionCooldownMoves);
        if (m.MovesSinceEcology >= cooldown)
        {
            EcologyAction act = MonsterEcologySystem.ResolveAfterMove(m, grid);
            m.ResetEcologyCounter();
            if (view != null)
            {
                if (act == EcologyAction.Absorbed) view.PlayCrawlingAction(m, true);
                else if (act == EcologyAction.Released) view.PlayCrawlingAction(m, false);
            }
        }

        if (MonsterLifecycleSystem.ResolveNaturalDeath(m, monsters, grid) == LifecycleOutcome.StarvationFailed)
            if (view != null) view.NotifyMonsterDied(m);
    }
}
