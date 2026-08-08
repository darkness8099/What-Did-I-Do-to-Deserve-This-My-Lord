using System.Collections.Generic;
using UnityEngine;

public class HeroManager : MonoBehaviour
{
    private GridManager gridManager;

    private Dictionary<int, HeroData>    heroes        = new Dictionary<int, HeroData>();
    private Dictionary<int, Vector2Int>  heroPositions = new Dictionary<int, Vector2Int>();
    private int nextHeroId = 0;

    private void Start()
    {
        gridManager = GetComponent<GridManager>();
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();

        if (gridManager == null)
            Debug.LogError("[HeroManager] GridManager not found in scene.");
        else
            Debug.Log("[HeroManager] Initialized.");
    }

    public int SpawnHeroAtEntrance()
    {
        return SpawnHeroAtEntrance(HeroArchetypeConfig.RuntimeDefault);
    }

    public int SpawnHeroAtEntrance(HeroArchetypeConfig archetype)
    {
        if (gridManager == null)
        {
            Debug.LogWarning("[HeroManager] SpawnHeroAtEntrance: GridManager is null.");
            return -1;
        }

        Vector2Int entrance = FindEntrance();
        if (entrance.x < 0)
        {
            Debug.LogWarning("[HeroManager] SpawnHeroAtEntrance: Entrance cell not found in GridData.");
            return -1;
        }

        int id = nextHeroId++;
        heroes[id]        = new HeroData(archetype);
        heroPositions[id] = entrance;

        Debug.Log($"[HeroManager] Hero #{id} ({heroes[id].ArchetypeId}) spawned at Entrance {entrance}.");
        return id;
    }

    public HeroData GetHero(int heroId)
    {
        heroes.TryGetValue(heroId, out var data);
        return data;
    }

    public Vector2Int GetHeroPosition(int heroId)
    {
        heroPositions.TryGetValue(heroId, out var pos);
        return pos;
    }

    public bool HasHero(int heroId)
    {
        return heroes.ContainsKey(heroId);
    }

    public IReadOnlyDictionary<int, HeroData> GetAllHeroes()
    {
        return heroes;
    }

    public bool SetHeroPosition(int heroId, Vector2Int newPos)
    {
        if (!heroPositions.ContainsKey(heroId)) return false;
        heroPositions[heroId] = newPos;
        return true;
    }

    public bool RemoveHero(int heroId)
    {
        if (!heroes.ContainsKey(heroId)) return false;
        heroes.Remove(heroId);
        heroPositions.Remove(heroId);
        return true;
    }

    public bool HasAnyHero()
    {
        return heroes.Count > 0;
    }

    public int HeroCount => heroes.Count;

    private Vector2Int FindEntrance()
    {
        return gridManager.GetEntrancePosition();
    }


public void CollectPositions(List<Vector2Int> buffer)
    {
        if (buffer == null) return;
        buffer.Clear();
        foreach (KeyValuePair<int, Vector2Int> pair in heroPositions)
            buffer.Add(pair.Value);
    }
}
