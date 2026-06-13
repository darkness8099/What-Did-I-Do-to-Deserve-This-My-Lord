using System.Collections.Generic;
using UnityEngine;

// Ecology tick driver — advances moss/slime ecology on a single fixed interval (NOT per-frame per monster).
// Per tick, for each monster (snapshot), dispatch by stage, fire death animations, then reconcile views once.
public class EcologyTickDriver : MonoBehaviour
{
    [SerializeField] private float tickSeconds = 1.0f;

    private MonsterManager monsters;
    private GridManager grid;
    private MonsterRenderer view;
    private float timer;
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
        Debug.Log("[EcologyTickDriver] Initialized.");
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < tickSeconds) return;
        timer -= tickSeconds;
        ProcessTick();
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
        Vector2Int cur;
        bool moved = MonsterMovementSystem.TryMoveStep(m, grid, out cur);
        if (moved) MonsterLifecycleSystem.ApplyMoveHpCost(m);

        EcologyAction act = MonsterEcologySystem.ResolveAfterMove(m, grid);
        if (view != null)
        {
            if (act == EcologyAction.Absorbed) view.PlayCrawlingAction(m, true);
            else if (act == EcologyAction.Released) view.PlayCrawlingAction(m, false);
        }

        if (MonsterLifecycleSystem.ResolveNaturalDeath(m, monsters) == LifecycleOutcome.StarvationFailed)
            if (view != null) view.NotifyMonsterDied(m);
    }
}
