using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HeroRouteState { GoingToDemonLordRoom, ReturningToEntrance }

public class HeroMover : MonoBehaviour
{
    private GridManager    gridManager;
    private HeroManager    heroManager;
    private HeroRenderer   heroRenderer;
    private CombatSystem   combatSystem;
    private MVPGameManager mvpGameManager;

    private static readonly Vector2Int DemonLordRoomPos = new Vector2Int(31, 9);
    private static readonly Vector2Int EntrancePos        = new Vector2Int(0,  9);

    private IEnumerator Start()
    {
        // Wait one frame so all sibling Start() calls complete first.
        yield return null;

        gridManager   = GetComponent<GridManager>()   ?? FindObjectOfType<GridManager>();
        heroManager   = GetComponent<HeroManager>()   ?? FindObjectOfType<HeroManager>();
        heroRenderer  = GetComponent<HeroRenderer>()  ?? FindObjectOfType<HeroRenderer>();
        combatSystem   = GetComponent<CombatSystem>()   ?? FindObjectOfType<CombatSystem>();
        mvpGameManager = GetComponent<MVPGameManager>() ?? FindObjectOfType<MVPGameManager>();

        if (gridManager    == null) { Debug.LogError("[HeroMover] GridManager not found.");    yield break; }
        if (heroManager    == null) { Debug.LogError("[HeroMover] HeroManager not found.");    yield break; }
        if (heroRenderer   == null) { Debug.LogError("[HeroMover] HeroRenderer not found.");   yield break; }
        if (combatSystem   == null) { Debug.LogError("[HeroMover] CombatSystem not found.");   yield break; }
        if (mvpGameManager == null) { Debug.LogError("[HeroMover] MVPGameManager not found."); yield break; }

        int heroId = heroManager.SpawnHeroAtEntrance();
        if (heroId < 0) { Debug.LogError("[HeroMover] Failed to spawn hero."); yield break; }

        heroRenderer.CreateHeroView(heroId);
        Debug.Log($"[HeroMover] Hero {heroId} spawned and view created. Starting movement coroutine.");
        StartCoroutine(MoveHero(heroId));
    }

    private IEnumerator MoveHero(int heroId)
    {
        var pathfinder = new HeroPathfinder(gridManager.GetGridData());
        HeroRouteState routeState = HeroRouteState.GoingToDemonLordRoom;

        while (true)
        {
            if (!heroManager.HasHero(heroId)) yield break;
            if (!mvpGameManager.IsPlaying()) yield break;

            Vector2Int currentPos = heroManager.GetHeroPosition(heroId);
            Vector2Int goal = (routeState == HeroRouteState.GoingToDemonLordRoom)
                ? DemonLordRoomPos : EntrancePos;

            if (currentPos == goal)
            {
                if (routeState == HeroRouteState.GoingToDemonLordRoom)
                {
                    Debug.Log($"[HeroMover] Hero {heroId} reached DemonLordRoom. Switching to return phase.");
                    mvpGameManager.NotifyHeroReachedDemonLordRoom(heroId);
                    routeState = HeroRouteState.ReturningToEntrance;
                }
                else
                {
                    mvpGameManager.NotifyHeroEscapedToEntrance(heroId);
                    yield break;
                }
                continue;
            }

            List<Vector2Int> path = pathfinder.FindPath(currentPos, goal);

            if (path == null || path.Count < 2)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            Vector2Int nextPos = path[1];
            yield return StartCoroutine(SmoothMove(heroId, currentPos, nextPos));
            heroManager.SetHeroPosition(heroId, nextPos);

            if (!combatSystem.ResolveCombatAt(heroId, nextPos))
            {
                mvpGameManager.NotifyHeroDefeated(heroId);
                yield break;
            }
        }
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
}
