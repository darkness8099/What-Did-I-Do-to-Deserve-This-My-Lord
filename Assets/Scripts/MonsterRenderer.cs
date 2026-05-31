using System.Collections.Generic;
using UnityEngine;

public class MonsterRenderer : MonoBehaviour
{
    private MonsterManager monsterManager;
    private Dictionary<Vector2Int, GameObject> views;
    private Transform viewsParent;
    [SerializeField] private Sprite spriteSlime;

private void Start()
    {
        monsterManager = GetComponent<MonsterManager>();
        if (monsterManager == null)
            monsterManager = FindObjectOfType<MonsterManager>();

        if (monsterManager == null)
        {
            Debug.LogError("[MonsterRenderer] MonsterManager not found in scene.");
            return;
        }

        views = new Dictionary<Vector2Int, GameObject>();

        var parentGO = new GameObject("MonsterViews");
        viewsParent = parentGO.transform;

        Debug.Log("[MonsterRenderer] Initialized.");
    }

    public bool HasMonsterView(int x, int y)
    {
        return views != null && views.ContainsKey(new Vector2Int(x, y));
    }

public void CreateMonsterView(int x, int y, MonsterData data)
    {
        if (HasMonsterView(x, y))
        {
            Debug.LogWarning($"[MonsterRenderer] CreateMonsterView: view already exists at ({x},{y}). Skipped.");
            return;
        }

        var go = new GameObject($"Slime_{x}_{y}");
        go.transform.SetParent(viewsParent, false);
        go.transform.position   = new Vector3(x + 0.5f, y + 0.5f, -0.1f);
        go.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = spriteSlime;
        sr.color        = Color.white;
        sr.sortingOrder = 0;

        views[new Vector2Int(x, y)] = go;
    }

    public GameObject GetMonsterView(int x, int y)
    {
        views.TryGetValue(new Vector2Int(x, y), out var go);
        return go;
    }

    public bool RemoveMonsterView(int x, int y)
    {
        var key = new Vector2Int(x, y);
        if (views == null || !views.TryGetValue(key, out var go)) return false;
        Destroy(go);
        views.Remove(key);
        return true;
    }


}
