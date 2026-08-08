using System.Collections.Generic;
using UnityEngine;

// Ecology tick driver — advances moss/slime ecology on a single fixed interval (NOT per-frame per monster).
// Per tick, for each monster (snapshot), dispatch by stage, fire death animations, then reconcile views once.
public class EcologyTickDriver : MonoBehaviour
{
    [SerializeField] private float tickSeconds = 1.0f;

    [Header("Simulation LOD")]
    [SerializeField] private bool enableSimulationLod = true;
    [SerializeField] private Camera simulationCamera;
    [SerializeField] private int exactPaddingCells = 4;
    [SerializeField] private int reducedPaddingCells = 12;
    [SerializeField] private int heroExactRadiusCells = 8;
    [SerializeField] private int heroReducedRadiusCells = 16;
    [SerializeField] private int reducedTickMultiplier = 4;
    [SerializeField] private float viewRefreshSeconds = 0.2f;
    [SerializeField] private int regionSize = 8;
    [SerializeField] private int aggregateTickMultiplier = 12;

    public int AggregateRegionCount => regionalSimulation != null ? regionalSimulation.RegionCount : 0;
    public int AggregateMonsterCount => regionalSimulation != null ? regionalSimulation.AggregateMonsterCount : 0;

    public int ExactIndividualCount { get; private set; }
    public int ReducedIndividualCount { get; private set; }
    public int AggregateCandidateCount { get; private set; }
    public int SimulationTickCount { get; private set; }

    [Header("Diagnostics (temporary)")]
    [SerializeField] private bool enableSlimeEcologyDiagnostics = true;
    [SerializeField] private int maxDiagnosticLines = 300;

    private const float GlobalLogSeconds = 5f;

    private MonsterManager monsters;
    private GridManager grid;
    private MonsterRenderer view;
    private HeroManager heroes;
    private DemonLordManager demonLordManager;
    private MonsterSimulationInterest currentInterest;
    private MonsterRegionalSimulation regionalSimulation;
    private float timer;
    private float globalTimer;
    private float viewTimer;
    private readonly List<MonsterData> scratch = new List<MonsterData>();
    private readonly List<MonsterData> visibleScratch = new List<MonsterData>();
    private readonly List<MonsterData> viewCandidates = new List<MonsterData>();
    private readonly List<Vector2Int> heroPositions = new List<Vector2Int>();
    private readonly HashSet<MonsterData> visibleUnique = new HashSet<MonsterData>();

    public bool IsSimulationPaused => ShouldPauseSimulation(demonLordManager);

private void Start()
    {
        monsters = GetComponent<MonsterManager>();
        if (monsters == null) monsters = FindObjectOfType<MonsterManager>();
        grid = GetComponent<GridManager>();
        if (grid == null) grid = FindObjectOfType<GridManager>();
        view = GetComponent<MonsterRenderer>();
        if (view == null) view = FindObjectOfType<MonsterRenderer>();
        heroes = GetComponent<HeroManager>();
        if (heroes == null) heroes = FindObjectOfType<HeroManager>();
        demonLordManager = GetComponent<DemonLordManager>();
        if (demonLordManager == null) demonLordManager = FindObjectOfType<DemonLordManager>();
        if (simulationCamera == null) simulationCamera = Camera.main;
        EnsureRegionalSimulation();

        if (monsters == null || grid == null)
        {
            Debug.LogError("[EcologyTickDriver] Missing MonsterManager/GridManager. Disabled.");
            enabled = false;
            return;
        }

        SlimeEcologyDiagnostics.Configure(enableSlimeEcologyDiagnostics, maxDiagnosticLines);
        SlimeEcologyDiagnostics.Begin();
        RebuildInterest();
        RefreshVisibleViews();

        Debug.Log("[EcologyTickDriver] Initialized with simulation LOD.");
    }

private void Update()
    {
        bool simulationPaused = IsSimulationPaused;
        if (view != null) view.SetSimulationPaused(simulationPaused);
        if (simulationPaused) return;

        viewTimer += Time.deltaTime;
        if (viewTimer >= Mathf.Max(0.05f, viewRefreshSeconds))
        {
            viewTimer = 0f;
            RefreshVisibleViews();
        }

        globalTimer += Time.deltaTime;
        if (globalTimer >= GlobalLogSeconds)
        {
            globalTimer -= GlobalLogSeconds;
            LogGlobal();
        }

        timer += Time.deltaTime;
        if (timer < tickSeconds) return;
        timer -= tickSeconds;
        ProcessSimulationTick();
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

public void ProcessSimulationTick()
    {
        if (IsSimulationPaused) return;

        SimulationTickCount++;
        RebuildInterest();
        EnsureRegionalSimulation();

        regionalSimulation.PromoteInterestedRegions(monsters, grid, currentInterest);
        monsters.CollectAll(scratch);

        bool aggregateDue = SimulationTickCount % Mathf.Max(1, aggregateTickMultiplier) == 0;
        if (aggregateDue)
        {
            regionalSimulation.CaptureFarCrawling(scratch, monsters, currentInterest, SimulationTickCount);
            regionalSimulation.Advance(
                Mathf.Max(1, aggregateTickMultiplier),
                SimulationTickCount,
                monsters,
                grid);
            monsters.CollectAll(scratch);
        }

        ExactIndividualCount = 0;
        ReducedIndividualCount = 0;
        AggregateCandidateCount = 0;
        bool reducedDue = SimulationTickCount % Mathf.Max(1, reducedTickMultiplier) == 0;

        for (int i = 0; i < scratch.Count; i++)
        {
            MonsterData monster = scratch[i];
            MonsterSimulationTier tier = GetTier(monster);

            if (monster.Stage != SlimeLifecycleStage.Crawling && tier == MonsterSimulationTier.Aggregate)
                tier = MonsterSimulationTier.Reduced;

            if (tier == MonsterSimulationTier.Exact)
            {
                ExactIndividualCount++;
                TickIndividual(monster, true);
            }
            else if (tier == MonsterSimulationTier.Reduced)
            {
                ReducedIndividualCount++;
                if (reducedDue) TickIndividual(monster, false);
            }
            else
            {
                // Far Crawling waits for the next aggregate capture boundary instead of paying an exact tick.
                AggregateCandidateCount++;
            }
        }

        RefreshVisibleViews();
    }

private void TickCrawling(MonsterData m, bool allowVisualFeedback)
    {
        if (m.IsSpawnDelayed()) return;

        Vector2Int cur;
        bool moved = MonsterMovementSystem.TryMoveStep(m, grid, monsters, out cur);
        if (moved) m.RegisterMove();

        // A monster may enter attack range during this movement step. Once contact is made,
        // combat takes priority over HP cost, nutrient transfer, and lifecycle resolution.
        if (ShouldSuspendForCombat(m, heroPositions)) return;

        int hpCd = Mathf.Max(1, m.Archetype.HpCostCooldownMoves);
        if (m.MovesSinceHpCost >= hpCd)
        {
            MonsterLifecycleSystem.ApplyMoveHpCost(m);
            m.ResetHpCostCounter();
        }

        int cooldown = Mathf.Max(1, m.Archetype.EcologyActionCooldownMoves);
        if (m.MovesSinceEcology >= cooldown)
        {
            EcologyAction act = MonsterEcologySystem.ResolveAfterMove(m, grid);
            m.ResetEcologyCounter();
            if (allowVisualFeedback && view != null)
            {
                if (act == EcologyAction.Absorbed) view.PlayCrawlingAction(m, true);
                else if (act == EcologyAction.Released) view.PlayCrawlingAction(m, false);
            }
        }

        if (MonsterLifecycleSystem.ResolveNaturalDeath(m, monsters, grid) == LifecycleOutcome.StarvationFailed)
            if (allowVisualFeedback && view != null) view.NotifyMonsterDied(m);
    }


private void RebuildInterest()
    {
        heroPositions.Clear();
        if (heroes != null) heroes.CollectPositions(heroPositions);

        currentInterest = MonsterSimulationPolicy.BuildInterest(
            simulationCamera,
            exactPaddingCells,
            reducedPaddingCells,
            heroPositions,
            heroExactRadiusCells,
            heroReducedRadiusCells);
    }

    private MonsterSimulationTier GetTier(MonsterData monster)
    {
        if (!enableSimulationLod || simulationCamera == null || monster == null)
            return MonsterSimulationTier.Exact;
        return MonsterSimulationPolicy.Classify(monster.Position, currentInterest);
    }

    private void RefreshVisibleViews()
    {
        if (view == null || monsters == null) return;
        if (!enableSimulationLod || simulationCamera == null)
        {
            view.SyncViews(monsters);
            return;
        }

        RebuildInterest();
        EnsureRegionalSimulation();
        regionalSimulation.PromoteInterestedRegions(monsters, grid, currentInterest);
        visibleScratch.Clear();
        visibleUnique.Clear();

        monsters.CollectInRect(currentInterest.ExactRect, viewCandidates);
        for (int i = 0; i < viewCandidates.Count; i++)
        {
            MonsterData monster = viewCandidates[i];
            if (visibleUnique.Add(monster)) visibleScratch.Add(monster);
        }

        int radius = Mathf.Max(0, heroExactRadiusCells);
        for (int h = 0; h < heroPositions.Count; h++)
        {
            Vector2Int hero = heroPositions[h];
            RectInt heroRect = new RectInt(hero.x - radius, hero.y - radius, radius * 2 + 1, radius * 2 + 1);
            monsters.CollectInRect(heroRect, viewCandidates);
            for (int i = 0; i < viewCandidates.Count; i++)
            {
                MonsterData monster = viewCandidates[i];
                if (GetTier(monster) == MonsterSimulationTier.Exact && visibleUnique.Add(monster))
                    visibleScratch.Add(monster);
            }
        }

        view.SyncVisible(visibleScratch);
    }

    private void TickIndividual(MonsterData monster, bool allowVisualFeedback)
    {
        if (ShouldSuspendForCombat(monster, heroPositions)) return;

        switch (monster.Stage)
        {
            case SlimeLifecycleStage.Crawling:
                TickCrawling(monster, allowVisualFeedback);
                break;

            case SlimeLifecycleStage.Bud:
                if (MonsterLifecycleSystem.BudTick(monster, monsters, grid) == StageTickOutcome.WitherFailed)
                    if (allowVisualFeedback && view != null) view.NotifyMonsterDied(monster);
                break;

            case SlimeLifecycleStage.Flower:
                if (MonsterLifecycleSystem.FlowerTick(monster, monsters, grid) == StageTickOutcome.Reproduced)
                    if (allowVisualFeedback && view != null) view.NotifyMonsterDied(monster);
                break;
        }
    }

    public static bool ShouldSuspendForCombat(MonsterData monster, IList<Vector2Int> heroCells)
    {
        if (monster == null || !monster.IsAlive() || heroCells == null) return false;

        float range = Mathf.Max(0f, monster.AttackRange);
        for (int i = 0; i < heroCells.Count; i++)
        {
            Vector2Int heroCell = heroCells[i];
            float distance = Mathf.Abs(monster.Position.x - heroCell.x)
                           + Mathf.Abs(monster.Position.y - heroCell.y);
            if (distance <= range) return true;
        }
        return false;
    }

    public static bool ShouldPauseSimulation(DemonLordManager manager)
    {
        return manager != null && manager.IsWaitingForPlacement;
    }


private void EnsureRegionalSimulation()
    {
        int size = Mathf.Max(1, regionSize);
        if (regionalSimulation == null || regionalSimulation.RegionSize != size)
            regionalSimulation = new MonsterRegionalSimulation(size);
    }

    public int EnsureExactAround(Vector2Int center, int radius)
    {
        if (regionalSimulation == null || monsters == null || grid == null) return 0;
        int promoted = 0;
        int r = Mathf.Max(0, radius);
        var visited = new HashSet<MonsterRegionKey>();

        for (int x = center.x - r; x <= center.x + r; x++)
        {
            for (int y = center.y - r; y <= center.y + r; y++)
            {
                MonsterRegionKey key = MonsterRegionKey.FromCell(new Vector2Int(x, y), regionalSimulation.RegionSize);
                if (!visited.Add(key)) continue;
                promoted += regionalSimulation.EnsureMaterializedAt(new Vector2Int(x, y), monsters, grid);
            }
        }
        return promoted;
    }
}
