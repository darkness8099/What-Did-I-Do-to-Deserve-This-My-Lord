using System.Collections.Generic;
using UnityEngine;

public class HeroRenderer : MonoBehaviour
{
    private HeroManager heroManager;
    private Dictionary<int, GameObject> views = new Dictionary<int, GameObject>();
    private Transform viewsParent;
    private Material matHero;

    private void Start()
    {
        heroManager = GetComponent<HeroManager>();
        if (heroManager == null)
            heroManager = FindObjectOfType<HeroManager>();
        if (heroManager == null)
            Debug.LogError("[HeroRenderer] HeroManager not found.");

        var parentGO = new GameObject("HeroViews");
        viewsParent = parentGO.transform;

        matHero = MakeMat(new Color(0.20f, 0.55f, 1.00f));
        Debug.Log("[HeroRenderer] Initialized.");
    }

    public bool HasHeroView(int heroId) => views.ContainsKey(heroId);

    public GameObject GetHeroView(int heroId)
    {
        views.TryGetValue(heroId, out var go);
        return go;
    }

    public void CreateHeroView(int heroId)
    {
        if (heroManager == null) return;

        if (!heroManager.HasHero(heroId))
        {
            Debug.LogWarning($"[HeroRenderer] CreateHeroView: heroId {heroId} not found in HeroManager.");
            return;
        }

        if (HasHeroView(heroId)) return;

        Vector2Int pos = heroManager.GetHeroPosition(heroId);

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = $"Hero_{heroId}";
        quad.transform.SetParent(viewsParent, false);
        quad.transform.position = new Vector3(pos.x + 0.5f, pos.y + 0.5f, -0.2f);
        quad.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        Destroy(quad.GetComponent<MeshCollider>());
        quad.GetComponent<MeshRenderer>().sharedMaterial = matHero;

        views[heroId] = quad;
        Debug.Log($"[HeroRenderer] Created view for Hero_{heroId} at grid ({pos.x},{pos.y}).");
    }

    public void SetHeroViewPosition(int heroId, Vector2Int gridPos)
    {
        if (!views.TryGetValue(heroId, out var go)) return;
        go.transform.position = new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, -0.2f);
    }

    public bool RemoveHeroView(int heroId)
    {
        if (!views.TryGetValue(heroId, out var go)) return false;
        Destroy(go);
        views.Remove(heroId);
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
