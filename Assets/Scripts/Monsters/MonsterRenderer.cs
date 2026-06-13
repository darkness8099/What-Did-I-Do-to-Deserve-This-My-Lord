using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Per-monster views (keyed by MonsterData reference, since multiple monsters may share a cell).
// Each tick SyncViews reconciles: create new, glide existing to their cell, restage on stage change, drop orphans.
// Deaths play a one-shot death animation before destroying the view (NotifyMonsterDied).
public class MonsterRenderer : MonoBehaviour
{
    private const string DefaultSlimePrefabPath = "Assets/Prefabs/Monsters/Slime.prefab";

    private MonsterManager monsterManager;
    private Dictionary<MonsterData, GameObject> views;
    private Dictionary<MonsterData, SlimeLifecycleStage> viewStages;
    private Transform viewsParent;

    [SerializeField] private GameObject slimePrefab;
    [SerializeField] private Sprite spriteSlime;
    [SerializeField] private RuntimeAnimatorController acCrawling;
    [SerializeField] private RuntimeAnimatorController acBud;
    [SerializeField] private RuntimeAnimatorController acFlower;

    [Header("View movement")]
    [SerializeField] private float viewMoveSpeed = 1.0f;   // cells/sec the view glides between cells
    [SerializeField] private float deathClipSeconds = 0.75f;

    private readonly List<MonsterData> tmp = new List<MonsterData>();
    private readonly List<MonsterData> tmpRemovals = new List<MonsterData>();

    private void Start()
    {
        monsterManager = GetComponent<MonsterManager>();
        if (monsterManager == null) monsterManager = FindObjectOfType<MonsterManager>();
        if (monsterManager == null)
        {
            Debug.LogError("[MonsterRenderer] MonsterManager not found in scene.");
            return;
        }

        views = new Dictionary<MonsterData, GameObject>();
        viewStages = new Dictionary<MonsterData, SlimeLifecycleStage>();

        var parentGO = new GameObject("MonsterViews");
        viewsParent = parentGO.transform;
        LoadDefaultPrefabIfNeeded();
        LoadStageControllersIfNeeded();

        Debug.Log("[MonsterRenderer] Initialized.");
    }

    private static Vector3 CellWorld(Vector2Int p) => new Vector3(p.x + 0.5f, p.y + 0.5f, -0.1f);

    public void CreateMonsterView(MonsterData data)
    {
        if (data == null || views == null) return;
        if (views.ContainsKey(data)) return;

        GameObject go = CreateViewInstance(data);
        go.transform.SetParent(viewsParent, false);
        go.name = $"{data.DisplayName}_{data.Position.x}_{data.Position.y}";
        go.transform.position = CellWorld(data.Position);
        go.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        var mover = go.GetComponent<MonsterViewMover>();
        if (mover == null) mover = go.AddComponent<MonsterViewMover>();
        mover.speed = viewMoveSpeed;
        mover.SnapTo(go.transform.position);

        views[data] = go;
        viewStages[data] = data.Stage;
        ApplyStageController(go, data.Stage);
    }

    // Reconcile views with current monsters: create missing, glide to cell, restage on change, drop orphans.
    public void SyncViews(MonsterManager mm)
    {
        if (mm == null || views == null) return;

        mm.CollectAll(tmp);
        var present = new HashSet<MonsterData>(tmp);

        foreach (MonsterData m in tmp)
        {
            if (!views.TryGetValue(m, out var go))
            {
                CreateMonsterView(m);
                continue;
            }

            Vector3 world = CellWorld(m.Position);
            var mover = go.GetComponent<MonsterViewMover>();

            SlimeLifecycleStage shown;
            bool stageChanged = !viewStages.TryGetValue(m, out shown) || shown != m.Stage;
            if (stageChanged)
            {
                viewStages[m] = m.Stage;
                ApplyStageController(go, m.Stage);
                // On transform (→Bud/Flower) snap to the cell so it stops and morphs on-grid (fix #2).
                if (mover != null) mover.SnapTo(world); else go.transform.position = world;
            }
            else
            {
                if (mover != null) mover.MoveTo(world); else go.transform.position = world; // glide
            }
        }

        tmpRemovals.Clear();
        foreach (var kv in views)
            if (!present.Contains(kv.Key)) tmpRemovals.Add(kv.Key);
        foreach (var k in tmpRemovals)
        {
            if (views.TryGetValue(k, out var go) && go != null) Destroy(go); // silent (death anim handled separately)
            views.Remove(k);
            viewStages.Remove(k);
        }
    }

    // Play a one-shot death animation for a dying monster's view, then destroy it (fix #3).
    // Call this just before/after removing the monster from MonsterManager.
    public void NotifyMonsterDied(MonsterData m)
    {
        if (views == null || m == null) return;
        if (!views.TryGetValue(m, out var go)) return;

        views.Remove(m);
        viewStages.Remove(m);
        if (go == null) return;

        var mover = go.GetComponent<MonsterViewMover>();
        if (mover != null) mover.SnapTo(go.transform.position); // stop gliding

        Animator anim = go.GetComponentInChildren<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null) anim.SetTrigger("Death");

        StartCoroutine(DestroyAfter(go, deathClipSeconds));
    }

    private IEnumerator DestroyAfter(GameObject go, float seconds)
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, seconds));
        if (go != null) Destroy(go);
    }

    // Play the Crawling absorb / emit one-shot (only AC_Slime has these triggers).
    public void PlayCrawlingAction(MonsterData m, bool absorb)
    {
        if (views == null || m == null) return;
        if (m.Stage != SlimeLifecycleStage.Crawling) return;
        if (!views.TryGetValue(m, out var go)) return;

        Animator anim = go.GetComponentInChildren<Animator>();
        if (anim == null || anim.runtimeAnimatorController == null) return;
        anim.SetTrigger(absorb ? "Absorb" : "Emit");
    }

    private void ApplyStageController(GameObject go, SlimeLifecycleStage stage)
    {
        if (go == null) return;
        Animator anim = go.GetComponentInChildren<Animator>();
        if (anim == null) return;

        RuntimeAnimatorController target =
            stage == SlimeLifecycleStage.Bud    ? acBud :
            stage == SlimeLifecycleStage.Flower ? acFlower :
                                                  acCrawling;

        if (target != null && anim.runtimeAnimatorController != target)
            anim.runtimeAnimatorController = target;
    }

    private GameObject CreateViewInstance(MonsterData data)
    {
        if (slimePrefab != null)
            return Instantiate(slimePrefab);

        var go = new GameObject(data.DisplayName);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteSlime;
        sr.color = Color.white;
        sr.sortingOrder = 0;
        return go;
    }

    private void LoadDefaultPrefabIfNeeded()
    {
        if (slimePrefab != null) return;
#if UNITY_EDITOR
        slimePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DefaultSlimePrefabPath);
        if (slimePrefab == null)
            slimePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PF_Monster_Slime_Default.prefab");
#endif
    }

    private void LoadStageControllersIfNeeded()
    {
#if UNITY_EDITOR
        if (acCrawling == null) acCrawling = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Monsters/AC_Slime.controller");
        if (acBud == null)      acBud      = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Monsters/AC_Bud.controller");
        if (acFlower == null)   acFlower   = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Monsters/AC_Flower.controller");
#endif
    }
}
