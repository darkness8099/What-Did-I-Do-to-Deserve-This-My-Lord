using System.Collections.Generic;
using UnityEngine;

public class MonsterRenderer : MonoBehaviour
{
    private MonsterManager monsterManager;
    private Dictionary<Vector2Int, GameObject> views;
    private Transform viewsParent;
    private Material matSlime;

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

        matSlime = MakeMat(new Color(1.0f, 0.85f, 0.0f));

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

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = $"Slime_{x}_{y}";
        quad.transform.SetParent(viewsParent, false);
        quad.transform.position = new Vector3(x + 0.5f, y + 0.5f, -0.1f);
        quad.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

        Destroy(quad.GetComponent<MeshCollider>());
        quad.GetComponent<MeshRenderer>().sharedMaterial = matSlime;

        views[new Vector2Int(x, y)] = quad;
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

    private static Material MakeMat(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        return mat;
    }
}
