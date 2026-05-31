using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Renders the surface decoration draft into 4 background layers.
// Does not save the scene and does not own placement generation rules.
public class BackgroundLayerRenderer : MonoBehaviour
{
    [SerializeField] private SurfaceDecorationSpawner surfaceDecorationSpawner;
    [SerializeField] private LevelConfig levelConfig;
    [SerializeField] private bool rebuildOnStart = true;
    [SerializeField] private string baseBackgroundPath = "Assets/Art/Backgrounds/bg_overworld_00.png";

    private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    private void Start()
    {
        if (rebuildOnStart)
            RebuildLayers();
    }

    public void RebuildLayers()
    {
        EnsureReferences();
        if (surfaceDecorationSpawner == null || levelConfig == null)
        {
            Debug.LogWarning("[BackgroundLayerRenderer] Missing LevelConfig or SurfaceDecorationSpawner.");
            return;
        }

        ClearLayers();

        IReadOnlyList<DecorationPlacementData> currentDraft = surfaceDecorationSpawner.CurrentDraft;
        List<DecorationPlacementData> draft = currentDraft != null && currentDraft.Count > 0
            ? new List<DecorationPlacementData>(currentDraft)
            : surfaceDecorationSpawner.GenerateDraft();

        SurfaceDecorationProfile profile = surfaceDecorationSpawner.Profile;
        if (profile == null)
        {
            Debug.LogWarning("[BackgroundLayerRenderer] SurfaceDecorationProfile is missing.");
            return;
        }

        CreateBaseBackground(profile);

        for (int i = 0; i < draft.Count; i++)
            CreateDecorationSprite(draft[i], i);

        Debug.Log($"[BackgroundLayerRenderer] Rendered {draft.Count} decoration sprites + base background.");
    }

    public void ClearLayers()
    {
        string[] layerRoots =
        {
            "BG_Base",
            "BG_BackDeco",
            "BG_MidDeco",
            "BG_FrontDeco",
        };

        for (int i = 0; i < layerRoots.Length; i++)
        {
            Transform child = transform.Find(layerRoots[i]);
            if (child != null)
                DestroyObjectSafe(child.gameObject);
        }
    }

    private void EnsureReferences()
    {
        if (surfaceDecorationSpawner == null)
            surfaceDecorationSpawner = GetComponent<SurfaceDecorationSpawner>();
        if (surfaceDecorationSpawner == null)
            surfaceDecorationSpawner = FindObjectOfType<SurfaceDecorationSpawner>();

        if (levelConfig == null)
            levelConfig = GetComponent<LevelConfig>();
        if (levelConfig == null)
            levelConfig = FindObjectOfType<LevelConfig>();
    }

    private void CreateBaseBackground(SurfaceDecorationProfile profile)
    {
        Sprite sprite = LoadSprite(baseBackgroundPath);
        if (sprite == null)
        {
            Debug.LogWarning($"[BackgroundLayerRenderer] Failed to load base background: {baseBackgroundPath}");
            return;
        }

        GameObject layerRoot = GetOrCreateLayerRoot("BG_Base");
        GameObject go = new GameObject("BG_Background");
        go.transform.SetParent(layerRoot.transform, false);
        go.transform.position = new Vector3(levelConfig.Width * 0.5f, levelConfig.Height - profile.SurfaceHeight * 0.5f, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = DecorationSortingLayer.BG_Base;
        sr.color = Color.white;
    }

    private void CreateDecorationSprite(DecorationPlacementData placement, int index)
    {
        Sprite sprite = LoadSprite(placement.SpritePath);
        if (sprite == null)
        {
            Debug.LogWarning($"[BackgroundLayerRenderer] Failed to load sprite: {placement.SpritePath}");
            return;
        }

        string layerName = ResolveLayerName(placement.SortingOrder);
        GameObject layerRoot = GetOrCreateLayerRoot(layerName);

        GameObject go = new GameObject($"{placement.Category}_{index:00}");
        go.transform.SetParent(layerRoot.transform, false);
        go.transform.position = new Vector3(
            placement.X + placement.FootprintWidth * 0.5f,
            levelConfig.Height - surfaceDecorationSpawner.Profile.SurfaceHeight,
            0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = placement.SortingOrder;
        sr.color = Color.white;
    }

    private GameObject GetOrCreateLayerRoot(string name)
    {
        Transform existing = transform.Find(name);
        if (existing != null) return existing.gameObject;

        GameObject root = new GameObject(name);
        root.transform.SetParent(transform, false);
        return root;
    }

    private string ResolveLayerName(int sortingOrder)
    {
        switch (sortingOrder)
        {
            case DecorationSortingLayer.BG_Base:
                return "BG_Base";
            case DecorationSortingLayer.BG_BackDeco:
                return "BG_BackDeco";
            case DecorationSortingLayer.BG_MidDeco:
                return "BG_MidDeco";
            case DecorationSortingLayer.BG_FrontDeco:
            default:
                return "BG_FrontDeco";
        }
    }

    private Sprite LoadSprite(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return null;
        if (spriteCache.TryGetValue(assetPath, out Sprite cached)) return cached;

        Sprite sprite = null;
#if UNITY_EDITOR
        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#endif
        spriteCache[assetPath] = sprite;
        return sprite;
    }

    private static void DestroyObjectSafe(GameObject go)
    {
        if (go == null) return;

        if (Application.isPlaying)
            Destroy(go);
        else
            DestroyImmediate(go);
    }
}
