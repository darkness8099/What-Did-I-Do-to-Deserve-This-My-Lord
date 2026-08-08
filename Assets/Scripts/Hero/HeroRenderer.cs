using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeroRenderer : MonoBehaviour
{
    private const string AnimatorControllerResourcePath = "Hero/anim_warrior_ctrl";
    private const string DefaultMotionState = "idle_s";

    private HeroManager heroManager;
    private Dictionary<int, GameObject> views    = new Dictionary<int, GameObject>();
    private Dictionary<int, Animator> animators = new Dictionary<int, Animator>();
    private Dictionary<int, string> motionStates = new Dictionary<int, string>();
    private Transform viewsParent;
    private RuntimeAnimatorController heroAnimatorController;
    [SerializeField] private Sprite spriteHero;

    private void Start()
    {
        heroManager = GetComponent<HeroManager>();
        if (heroManager == null)
            heroManager = FindObjectOfType<HeroManager>();
        if (heroManager == null)
            Debug.LogError("[HeroRenderer] HeroManager not found.");

        heroAnimatorController = Resources.Load<RuntimeAnimatorController>(AnimatorControllerResourcePath);
        if (heroAnimatorController == null)
            Debug.LogError($"[HeroRenderer] AnimatorController not found at Resources/{AnimatorControllerResourcePath}.");

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
        HeroData heroData = heroManager.GetHero(heroId);

        var go = new GameObject($"Hero_{heroId}");
        go.transform.SetParent(viewsParent, false);
        go.transform.position   = new Vector3(pos.x + 0.5f, pos.y + 0.5f, -0.2f);
        go.transform.localScale = Vector3.one;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        // The Hero root represents the cell center, while character sprites use a
        // Bottom Center pivot. Shift only the visual down so its feet sit on the
        // cell's lower edge without changing the logical/grid-centered root.
        visual.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        visual.transform.localScale = Vector3.one;

        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite       = heroData != null && heroData.Archetype != null && heroData.Archetype.Sprite != null
            ? heroData.Archetype.Sprite
            : spriteHero;
        sr.color        = Color.white;
        sr.sortingOrder = 1;

        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = heroData != null
            && heroData.Archetype != null
            && heroData.Archetype.AnimatorController != null
                ? heroData.Archetype.AnimatorController
                : heroAnimatorController;
        animator.applyRootMotion = false;

        views[heroId] = go;
        animators[heroId] = animator;
        motionStates[heroId] = DefaultMotionState;
        if (heroAnimatorController != null)
            animator.Play(DefaultMotionState, 0, 0f);
        Debug.Log($"[HeroRenderer] Created view for Hero_{heroId} at grid ({pos.x},{pos.y}).");
    }

    public void SetHeroMotion(int heroId, Vector2Int direction, bool moving)
    {
        if (!animators.TryGetValue(heroId, out var animator) || animator == null)
            return;

        string stateName = GetMotionStateName(direction, moving);
        if (motionStates.TryGetValue(heroId, out var currentState) && currentState == stateName)
            return;

        motionStates[heroId] = stateName;
        if (animator.runtimeAnimatorController != null)
            animator.Play(stateName, 0, 0f);
    }

    public static string GetMotionStateName(Vector2Int direction, bool moving)
    {
        string suffix;
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y) && direction.x != 0)
            suffix = direction.x > 0 ? "e" : "w";
        else if (direction.y != 0)
            suffix = direction.y > 0 ? "n" : "s";
        else
            suffix = "s";

        return (moving ? "walk_" : "idle_") + suffix;
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
        animators.Remove(heroId);
        motionStates.Remove(heroId);
        return true;
    }
}
