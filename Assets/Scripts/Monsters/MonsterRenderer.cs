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
    private readonly HashSet<MonsterData> visibleSet = new HashSet<MonsterData>();
    private readonly Stack<GameObject> pooledViews = new Stack<GameObject>();
    private readonly Dictionary<Animator, PausedAnimatorPlayback> pausedAnimatorPlaybacks =
        new Dictionary<Animator, PausedAnimatorPlayback>();
    private readonly List<Animator> pausedAnimatorRemovals = new List<Animator>();

    private bool simulationPaused;

    private sealed class PausedAnimatorPlayback
    {
        public int StateHash;
        public float NormalizedTime;
        public float ClipLength;
        public float EffectiveSpeed;
        public float OriginalAnimatorSpeed;
    }

    public int ActiveViewCount => views != null ? views.Count : 0;
    public int PooledViewCount => pooledViews.Count;
    public bool IsSimulationPaused => simulationPaused;
    public int PausedAnimatorCount => pausedAnimatorPlaybacks.Count;

    public GameObject GetMonsterView(MonsterData monster)
    {
        if (views == null || monster == null) return null;
        views.TryGetValue(monster, out GameObject view);
        return view;
    }

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

    private void Update()
    {
        AdvancePausedVisuals(Time.deltaTime);
    }

    private static Vector3 CellWorld(Vector2Int p) => new Vector3(p.x + 0.5f, p.y + 0.5f, -0.1f);

public void CreateMonsterView(MonsterData data)
    {
        if (data == null || views == null) return;
        if (views.ContainsKey(data)) return;

        GameObject go = CreateViewInstance(data);
        go.transform.SetParent(viewsParent, false);
        go.SetActive(true);
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

        Animator animator = go.GetComponentInChildren<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        if (simulationPaused) SetViewSimulationPaused(go, true);
    }

    // Reconcile views with current monsters: create missing, glide to cell, restage on change, drop orphans.
public void SyncViews(MonsterManager mm)
    {
        if (mm == null || views == null) return;
        mm.CollectAll(tmp);
        SyncVisible(tmp);
    }

public void SyncVisible(IList<MonsterData> visibleMonsters)
    {
        if (views == null) return;

        visibleSet.Clear();
        if (visibleMonsters != null)
        {
            for (int i = 0; i < visibleMonsters.Count; i++)
            {
                MonsterData m = visibleMonsters[i];
                if (m == null) continue;
                visibleSet.Add(m);

                GameObject go;
                if (!views.TryGetValue(m, out go))
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
                    if (mover != null) mover.SnapTo(world); else go.transform.position = world;
                }
                else
                {
                    if (mover != null) mover.MoveTo(world); else go.transform.position = world;
                }
            }
        }

        tmpRemovals.Clear();
        foreach (KeyValuePair<MonsterData, GameObject> pair in views)
            if (!visibleSet.Contains(pair.Key)) tmpRemovals.Add(pair.Key);

        for (int i = 0; i < tmpRemovals.Count; i++)
        {
            MonsterData key = tmpRemovals[i];
            GameObject go;
            if (views.TryGetValue(key, out go)) ReleaseToPool(go);
            views.Remove(key);
            viewStages.Remove(key);
        }
    }

    private void ReleaseToPool(GameObject go)
    {
        if (go == null) return;
        SetViewSimulationPaused(go, false);
        go.SetActive(false);
        go.name = "PooledMonsterView";
        go.transform.SetParent(viewsParent, false);
        pooledViews.Push(go);
    }

    // Freezes data-driven root movement while manually looping the currently visible Animator state.
    // Animator transitions do not advance during the pause; the captured state resumes afterwards.
    public void SetSimulationPaused(bool paused)
    {
        if (simulationPaused == paused) return;
        simulationPaused = paused;

        if (views != null)
        {
            foreach (KeyValuePair<MonsterData, GameObject> pair in views)
                SetViewSimulationPaused(pair.Value, paused);
        }

        if (!paused) ResumeAllPausedAnimators();
    }

    public void AdvancePausedVisuals(float deltaTime)
    {
        if (!simulationPaused || deltaTime <= 0f || pausedAnimatorPlaybacks.Count == 0) return;

        pausedAnimatorRemovals.Clear();
        foreach (KeyValuePair<Animator, PausedAnimatorPlayback> pair in pausedAnimatorPlaybacks)
        {
            Animator animator = pair.Key;
            PausedAnimatorPlayback playback = pair.Value;
            if (animator == null)
            {
                pausedAnimatorRemovals.Add(animator);
                continue;
            }
            if (!animator.isActiveAndEnabled) continue;

            playback.NormalizedTime = AdvanceLoopedNormalizedTime(
                playback.NormalizedTime,
                deltaTime,
                playback.ClipLength,
                playback.EffectiveSpeed);
            animator.Play(playback.StateHash, 0, playback.NormalizedTime);
            animator.Update(0f);
        }

        for (int i = 0; i < pausedAnimatorRemovals.Count; i++)
            pausedAnimatorPlaybacks.Remove(pausedAnimatorRemovals[i]);
    }

    public static float AdvanceLoopedNormalizedTime(
        float normalizedTime,
        float deltaTime,
        float clipLength,
        float playbackSpeed)
    {
        float safeLength = Mathf.Max(0.0001f, clipLength);
        return Mathf.Repeat(normalizedTime + deltaTime * playbackSpeed / safeLength, 1f);
    }

    private void SetViewSimulationPaused(GameObject go, bool paused)
    {
        if (go == null) return;

        MonsterViewMover mover = go.GetComponent<MonsterViewMover>();
        if (mover != null) mover.SetMovementPaused(paused);

        Animator animator = go.GetComponentInChildren<Animator>();
        if (paused) CapturePausedAnimator(animator);
        else ResumePausedAnimator(animator);
    }

    private void CapturePausedAnimator(Animator animator)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        if (pausedAnimatorPlaybacks.ContainsKey(animator) || animator.layerCount <= 0) return;

        bool inTransition = animator.IsInTransition(0);
        AnimatorStateInfo state = inTransition
            ? animator.GetNextAnimatorStateInfo(0)
            : animator.GetCurrentAnimatorStateInfo(0);
        if (state.fullPathHash == 0) return;

        AnimatorClipInfo[] clips = inTransition
            ? animator.GetNextAnimatorClipInfo(0)
            : animator.GetCurrentAnimatorClipInfo(0);
        float clipLength = clips.Length > 0 && clips[0].clip != null
            ? clips[0].clip.length
            : Mathf.Max(0.0001f, state.length);

        var playback = new PausedAnimatorPlayback
        {
            StateHash = state.fullPathHash,
            NormalizedTime = Mathf.Repeat(state.normalizedTime, 1f),
            ClipLength = Mathf.Max(0.0001f, clipLength),
            EffectiveSpeed = animator.speed * state.speed * state.speedMultiplier,
            OriginalAnimatorSpeed = animator.speed,
        };

        pausedAnimatorPlaybacks.Add(animator, playback);
        animator.speed = 0f;
        animator.Play(playback.StateHash, 0, playback.NormalizedTime);
        animator.Update(0f);
    }

    private void ResumePausedAnimator(Animator animator)
    {
        if (animator == null) return;
        if (!pausedAnimatorPlaybacks.TryGetValue(animator, out PausedAnimatorPlayback playback)) return;

        animator.speed = playback.OriginalAnimatorSpeed;
        if (animator.runtimeAnimatorController != null)
        {
            animator.Play(playback.StateHash, 0, playback.NormalizedTime);
            animator.Update(0f);
        }
        pausedAnimatorPlaybacks.Remove(animator);
    }

    private void ResumeAllPausedAnimators()
    {
        pausedAnimatorRemovals.Clear();
        foreach (KeyValuePair<Animator, PausedAnimatorPlayback> pair in pausedAnimatorPlaybacks)
        {
            Animator animator = pair.Key;
            PausedAnimatorPlayback playback = pair.Value;
            if (animator != null)
            {
                animator.speed = playback.OriginalAnimatorSpeed;
                if (animator.runtimeAnimatorController != null)
                {
                    animator.Play(playback.StateHash, 0, playback.NormalizedTime);
                    animator.Update(0f);
                }
            }
            pausedAnimatorRemovals.Add(animator);
        }

        for (int i = 0; i < pausedAnimatorRemovals.Count; i++)
            pausedAnimatorPlaybacks.Remove(pausedAnimatorRemovals[i]);
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

        StartCoroutine(ReleaseAfter(go, deathClipSeconds));
    }

private IEnumerator ReleaseAfter(GameObject go, float seconds)
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, seconds));
        if (go != null) ReleaseToPool(go);
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
        if (pooledViews.Count > 0)
            return pooledViews.Pop();

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
