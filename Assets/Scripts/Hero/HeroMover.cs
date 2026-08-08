using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HeroRouteState { GoingToDemonLord, ReturningToEntrance }

public class HeroMover : MonoBehaviour
{
    private LevelConfig levelConfig;
    private GridManager    gridManager;
    private DemonLordManager demonLordManager;
    private DemonLordRenderer demonLordRenderer;
    private HeroManager    heroManager;
    private HeroRenderer   heroRenderer;
    private CombatSystem   combatSystem;
    private MVPGameManager mvpGameManager;
    private MonsterManager  monsterManager;
    private EcologyTickDriver ecologyTickDriver;

    private static readonly Vector2Int[] CaptureOffsets =
    {
        new Vector2Int( 0,  1),
        new Vector2Int( 0, -1),
        new Vector2Int(-1,  0),
        new Vector2Int( 1,  0),
    };

    private IEnumerator Start()
    {
        // Wait one frame so all sibling Start() calls complete first.
        yield return null;

        levelConfig = GetComponent<LevelConfig>() ?? FindObjectOfType<LevelConfig>();
        gridManager   = GetComponent<GridManager>()   ?? FindObjectOfType<GridManager>();
        demonLordManager = GetComponent<DemonLordManager>() ?? FindObjectOfType<DemonLordManager>();
        demonLordRenderer = GetComponent<DemonLordRenderer>() ?? FindObjectOfType<DemonLordRenderer>();
        heroManager   = GetComponent<HeroManager>()   ?? FindObjectOfType<HeroManager>();
        heroRenderer  = GetComponent<HeroRenderer>()  ?? FindObjectOfType<HeroRenderer>();
        combatSystem   = GetComponent<CombatSystem>()   ?? FindObjectOfType<CombatSystem>();
        mvpGameManager = GetComponent<MVPGameManager>() ?? FindObjectOfType<MVPGameManager>();

        if (levelConfig == null) { Debug.LogError("[HeroMover] LevelConfig not found."); yield break; }
        if (gridManager    == null) { Debug.LogError("[HeroMover] GridManager not found.");    yield break; }
        if (demonLordManager == null) { Debug.LogError("[HeroMover] DemonLordManager not found."); yield break; }
        if (demonLordRenderer == null) { Debug.LogError("[HeroMover] DemonLordRenderer not found."); yield break; }
        if (heroManager    == null) { Debug.LogError("[HeroMover] HeroManager not found.");    yield break; }
        if (heroRenderer   == null) { Debug.LogError("[HeroMover] HeroRenderer not found.");   yield break; }
        if (combatSystem   == null) { Debug.LogError("[HeroMover] CombatSystem not found.");   yield break; }
        if (mvpGameManager == null) { Debug.LogError("[HeroMover] MVPGameManager not found."); yield break; }

        monsterManager = GetComponent<MonsterManager>() ?? FindObjectOfType<MonsterManager>();
        ecologyTickDriver = GetComponent<EcologyTickDriver>() ?? FindObjectOfType<EcologyTickDriver>();
        if (monsterManager  == null) { Debug.LogError("[HeroMover] MonsterManager not found.");  yield break; }

        HeroWaveDirector waveDirector = GetComponent<HeroWaveDirector>();
        if (waveDirector == null)
            waveDirector = gameObject.AddComponent<HeroWaveDirector>();
        waveDirector.Initialize(
            levelConfig,
            demonLordManager,
            heroManager,
            heroRenderer,
            this,
            mvpGameManager);
    }

    public bool StartHero(int heroId)
    {
        if (heroManager == null || !heroManager.HasHero(heroId)) return false;
        StartCoroutine(MoveHero(heroId));
        return true;
    }

    private IEnumerator MoveHero(int heroId)
    {
        var pathfinder = new HeroPathfinder(gridManager.GetGridData());
        HeroRouteState routeState = HeroRouteState.GoingToDemonLord;

        while (true)
        {
            if (!heroManager.HasHero(heroId)) yield break;
            if (!mvpGameManager.IsPlaying()) yield break;

            Vector2Int currentPos = heroManager.GetHeroPosition(heroId);
            HeroData   heroData   = heroManager.GetHero(heroId);
            Vector2Int demonLordPos = demonLordManager.GetPosition();
            Vector2Int entrancePos = gridManager.GetEntrancePosition();

            // 靠近魔王单位一格即捕获；返程到达入口则逃脱。
            bool atGoal = (routeState == HeroRouteState.GoingToDemonLord)
                ? (Mathf.Abs(currentPos.x - demonLordPos.x) + Mathf.Abs(currentPos.y - demonLordPos.y) <= 1)
                : (currentPos == entrancePos);

            if (atGoal)
            {
                heroRenderer.SetHeroMotion(heroId, heroData.FacingDirection, false);
                if (routeState == HeroRouteState.GoingToDemonLord)
                {
                    if (demonLordManager.Capture(heroId))
                    {
                        Debug.Log($"[HeroMover] Hero {heroId} captured DemonLord. Returning to entrance.");
                        mvpGameManager.NotifyHeroReachedDemonLord(heroId);
                        demonLordRenderer.AttachCaptiveDemonLord(heroId, demonLordPos);
                        routeState = HeroRouteState.ReturningToEntrance;
                    }
                    else
                    {
                        // Another hero is already carrying the DemonLord. Keep pursuing its live position.
                        yield return null;
                    }
                }
                else
                {
                    mvpGameManager.NotifyHeroEscapedToEntrance(heroId);
                    yield break;
                }
                continue;
            }

            // 索敌：攻击范围内有怪物则停下战斗
            if (ecologyTickDriver != null)
                ecologyTickDriver.EnsureExactAround(currentPos, Mathf.CeilToInt(heroData.AttackRange));

            MonsterData target = monsterManager.FindNearestMonsterTargetInRange(currentPos, heroData.AttackRange);
            if (target != null)
            {
                heroRenderer.SetHeroMotion(heroId, heroData.FacingDirection, false);
                var combatResult = new CombatResult();
                yield return StartCoroutine(combatSystem.ResolveCombat(heroId, target, combatResult));
                if (!combatResult.HeroSurvived)
                {
                    DropDemonLordIfCaptor(heroId);
                    mvpGameManager.NotifyHeroDefeated(heroId);
                    yield break;
                }
                continue;
            }

            // 寻路移动一格
            List<Vector2Int> path = (routeState == HeroRouteState.GoingToDemonLord)
                ? FindPathToDemonLordCaptureCell(pathfinder, currentPos, demonLordPos)
                : pathfinder.FindPath(currentPos, entrancePos);
            if (path == null || path.Count < 2)
            {
                heroRenderer.SetHeroMotion(heroId, heroData.FacingDirection, false);
                yield return new WaitForSeconds(1f);
                continue;
            }

            Vector2Int nextPos = path[1];
            Vector2Int prevPos = currentPos;
            float moveDur = heroData.MoveSpeed > 0f ? 1f / heroData.MoveSpeed : 0.5f;

            // 更新面向方向（为将来面向索敌预留）
            heroData.SetFacingDirection(new Vector2Int(
                System.Math.Sign(nextPos.x - prevPos.x),
                System.Math.Sign(nextPos.y - prevPos.y)));
            heroRenderer.SetHeroMotion(heroId, heroData.FacingDirection, true);

            // 返程：魔王同步平移至勇者前一格
            if (routeState == HeroRouteState.ReturningToEntrance)
                StartCoroutine(demonLordRenderer.SmoothMoveCaptive(heroId, prevPos, moveDur));

            yield return StartCoroutine(SmoothMove(heroId, currentPos, nextPos));
            heroManager.SetHeroPosition(heroId, nextPos);
            if (routeState == HeroRouteState.ReturningToEntrance)
                demonLordManager.UpdateCapturedPosition(heroId, prevPos);
        }
    }

    private void DropDemonLordIfCaptor(int heroId)
    {
        if (!demonLordManager.IsCaptured || demonLordManager.CaptorHeroId != heroId) return;

        Vector2Int dropPosition = demonLordManager.GetPosition();
        if (demonLordManager.ReleaseCaptureAt(heroId, dropPosition))
            demonLordRenderer.DropCaptiveDemonLord(heroId, dropPosition);
    }

    private IEnumerator SmoothMove(int heroId, Vector2Int from, Vector2Int to)
    {
        HeroData data = heroManager.GetHero(heroId);
        if (data == null) yield break;

        GameObject view = heroRenderer.GetHeroView(heroId);
        if (view == null) yield break;

        float duration = 1f / data.MoveSpeed;
        Vector3 startWorld = new Vector3(from.x + 0.5f, from.y + 0.5f, -0.2f);
        Vector3 endWorld   = new Vector3(to.x   + 0.5f, to.y   + 0.5f, -0.2f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            view.transform.position = Vector3.Lerp(startWorld, endWorld,
                                          Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        view.transform.position = endWorld;
    }

    private List<Vector2Int> FindPathToDemonLordCaptureCell(
        HeroPathfinder pathfinder,
        Vector2Int start,
        Vector2Int demonLordPos)
    {
        List<Vector2Int> bestPath = null;

        foreach (Vector2Int offset in CaptureOffsets)
        {
            Vector2Int target = demonLordPos + offset;
            List<Vector2Int> path = pathfinder.FindPath(start, target);
            if (path == null) continue;

            if (bestPath == null || path.Count < bestPath.Count)
                bestPath = path;
        }

        return bestPath;
    }
}
