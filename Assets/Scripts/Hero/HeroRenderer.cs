using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeroRenderer : MonoBehaviour
{
    private HeroManager heroManager;
    private Dictionary<int, GameObject> views    = new Dictionary<int, GameObject>();
    private Transform viewsParent;
    [SerializeField] private Sprite spriteHero;

private void Start()
    {
        heroManager = GetComponent<HeroManager>();
        if (heroManager == null)
            heroManager = FindObjectOfType<HeroManager>();
        if (heroManager == null)
            Debug.LogError("[HeroRenderer] HeroManager not found.");

        var parentGO = new GameObject("HeroViews");
        viewsParent = parentGO.transform;

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

        var go = new GameObject($"Hero_{heroId}");
        go.transform.SetParent(viewsParent, false);
        go.transform.position   = new Vector3(pos.x + 0.5f, pos.y + 0.5f, -0.2f);
        go.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = spriteHero;
        sr.color        = Color.white;
        sr.sortingOrder = 1;

        views[heroId] = go;
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
}
