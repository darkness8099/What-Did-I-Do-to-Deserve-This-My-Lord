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
    [SerializeField] private string baseBackgroundPath = "Assets/Art/Backgrounds/bg_overworld_00.png";
    [SerializeField] private string savedPrefabFolder = "Assets/Prefabs/Backgrounds";
    [SerializeField] private string savedPrefabNamePrefix = "PF_Background_Surface";
    [SerializeField] private int savedPrefabMaxCount = 10;
    [SerializeField] private bool loadRandomSavedPrefabOnStart = true;
    [SerializeField] private float decorationBaselineOffsetCells = 1f;

    private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private GameObject runtimeBackgroundInstance;

    private void Start()
    {
        if (loadRandomSavedPrefabOnStart)
            LoadRandomSavedBackgroundForGameplay();
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

    public void GenerateRandomSeed()
    {
        EnsureReferences();
        if (surfaceDecorationSpawner == null)
        {
            Debug.LogWarning("[BackgroundLayerRenderer] SurfaceDecorationSpawner is missing.");
            return;
        }

        surfaceDecorationSpawner.RandomSeed = UnityEngine.Random.Range(1, int.MaxValue);
        MarkDirty();
        Debug.Log($"[BackgroundLayerRenderer] Random seed set to {surfaceDecorationSpawner.RandomSeed}.");
    }

    public void GenerateDraftInEditor()
    {
        EnsureReferences();
        if (surfaceDecorationSpawner == null)
        {
            Debug.LogWarning("[BackgroundLayerRenderer] SurfaceDecorationSpawner is missing.");
            return;
        }

        surfaceDecorationSpawner.RegenerateDraft();
        RebuildLayers();
    }

    public void ClearGeneratedBackground()
    {
        EnsureReferences();
        if (surfaceDecorationSpawner != null)
            surfaceDecorationSpawner.ClearDraft();

        ClearLayers();
        MarkDirty();
        Debug.Log("[BackgroundLayerRenderer] Cleared generated background layers and draft.");
    }

    public bool SaveCurrentBackgroundAsPrefab()
    {
#if UNITY_EDITOR
        EnsureReferences();

        string savePath = GetNextAvailablePrefabPath();
        if (string.IsNullOrEmpty(savePath))
        {
            return false;
        }

        GameObject tempRoot = new GameObject(System.IO.Path.GetFileNameWithoutExtension(savePath));
        try
        {
            string[] layerRoots =
            {
                "BG_Base",
                "BG_BackDeco",
                "BG_MidDeco",
                "BG_FrontDeco",
                "BG_TopDeco",
            };

            for (int i = 0; i < layerRoots.Length; i++)
            {
                Transform child = transform.Find(layerRoots[i]);
                if (child != null)
                {
                    GameObject clone = Instantiate(child.gameObject);
                    clone.name = child.gameObject.name;
                    clone.transform.SetParent(tempRoot.transform, false);
                }
            }

            if (tempRoot.transform.childCount == 0)
            {
                Debug.LogWarning("[BackgroundLayerRenderer] Nothing to save. Generate background first.");
                return false;
            }

            PrefabUtility.SaveAsPrefabAsset(tempRoot, savePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BackgroundLayerRenderer] Saved background prefab: {savePath}");
            return true;
        }
        finally
        {
            DestroyObjectSafe(tempRoot);
        }
#else
        Debug.LogWarning("[BackgroundLayerRenderer] SaveCurrentBackgroundAsPrefab is editor-only.");
        return false;
#endif
    }

    public bool LoadRandomSavedBackgroundForGameplay()
    {
#if UNITY_EDITOR
        GameObject prefab = PickRandomSavedBackgroundPrefab();
        if (prefab == null)
        {
            Debug.LogError($"[BackgroundLayerRenderer] No saved background prefab found in {savedPrefabFolder} with prefix {savedPrefabNamePrefix}.");
            return false;
        }

        ClearLayers();
        if (runtimeBackgroundInstance != null)
            DestroyObjectSafe(runtimeBackgroundInstance);

        runtimeBackgroundInstance = Instantiate(prefab, transform);
        runtimeBackgroundInstance.name = $"{prefab.name}_Runtime";
        runtimeBackgroundInstance.transform.localPosition = Vector3.zero;
        runtimeBackgroundInstance.transform.localRotation = Quaternion.identity;
        runtimeBackgroundInstance.transform.localScale = Vector3.one;

        Debug.Log($"[BackgroundLayerRenderer] Loaded gameplay background prefab: {prefab.name}");
        return true;
#else
        Debug.LogError("[BackgroundLayerRenderer] Saved background prefab folder loading currently requires the Unity Editor.");
        return false;
#endif
    }

#if UNITY_EDITOR
    private GameObject PickRandomSavedBackgroundPrefab()
    {
        List<GameObject> prefabs = new List<GameObject>();
        int maxCount = Mathf.Max(1, savedPrefabMaxCount);

        for (int i = 1; i <= maxCount; i++)
        {
            string path = $"{savedPrefabFolder}/{savedPrefabNamePrefix}_{i:00}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                prefabs.Add(prefab);
        }

        if (prefabs.Count == 0) return null;
        return prefabs[UnityEngine.Random.Range(0, prefabs.Count)];
    }

    private string GetNextAvailablePrefabPath()
    {
        if (string.IsNullOrEmpty(savedPrefabFolder))
        {
            Debug.LogWarning("[BackgroundLayerRenderer] Saved prefab folder is empty.");
            return null;
        }

        if (string.IsNullOrEmpty(savedPrefabNamePrefix))
        {
            Debug.LogWarning("[BackgroundLayerRenderer] Saved prefab name prefix is empty.");
            return null;
        }

        EnsureAssetFolder(savedPrefabFolder);

        int maxCount = Mathf.Max(1, savedPrefabMaxCount);
        for (int i = 1; i <= maxCount; i++)
        {
            string path = $"{savedPrefabFolder}/{savedPrefabNamePrefix}_{i:00}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                return path;
        }

        Debug.LogWarning($"[BackgroundLayerRenderer] No available background prefab slot in {savedPrefabFolder}. Max count: {maxCount}.");
        return null;
    }

    private static void EnsureAssetFolder(string folderPath)
    {
        string normalized = folderPath.Replace("\\", "/").Trim('/');
        string[] parts = normalized.Split('/');
        if (parts.Length == 0 || parts[0] != "Assets") return;

        string current = "Assets";
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
#endif

    public void ClearLayers()
    {
        string[] layerRoots =
        {
            "BG_Base",
            "BG_BackDeco",
            "BG_MidDeco",
            "BG_FrontDeco",
            "BG_TopDeco",
        };

        for (int i = 0; i < layerRoots.Length; i++)
        {
            Transform child = transform.Find(layerRoots[i]);
            if (child != null)
                DestroyObjectSafe(child.gameObject);
        }

        MarkDirty();
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
            GetDecorationBaselineY(),
            0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = placement.SortingOrder;
        sr.color = Color.white;
    }

    private float GetDecorationBaselineY()
    {
        return levelConfig.Height - surfaceDecorationSpawner.Profile.SurfaceHeight + decorationBaselineOffsetCells;
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
                return "BG_FrontDeco";
            case DecorationSortingLayer.BG_TopDeco:
                return "BG_TopDeco";
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

    private void MarkDirty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorUtility.SetDirty(gameObject);
#endif
    }
}
