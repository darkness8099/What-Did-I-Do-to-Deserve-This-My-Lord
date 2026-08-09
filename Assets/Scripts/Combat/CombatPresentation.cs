using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Lightweight combat feedback for the demo. It only moves each view's Visual child;
// logical roots and grid positions remain authoritative.
public static class CombatPresentation
{
    public const float DefaultPhaseSeconds = 0.22f;
    public const float DefaultLungeDistance = 0.22f;

    private const string WhiteFlashMaterialPath = "Combat/mat_sprite_white_flash";
    private const float ForwardRatio = 0.36f;
    private const float FlashHoldRatio = 0.27f;

    private static readonly HashSet<Transform> activeLungeVisuals = new HashSet<Transform>();
    private static readonly Dictionary<SpriteRenderer, FlashState> activeFlashes =
        new Dictionary<SpriteRenderer, FlashState>();

    private static Material whiteFlashMaterial;
    private static bool whiteFlashMaterialChecked;

    private sealed class LungeState
    {
        public Transform Visual;
        public Vector3 BaseLocalPosition;
        public Vector3 LungeLocalOffset;
    }

    private sealed class FlashState
    {
        public Material OriginalMaterial;
        public int ReferenceCount;
    }

    public static float GetPhaseDuration(float attackInterval)
    {
        return Mathf.Min(DefaultPhaseSeconds, Mathf.Max(0.01f, attackInterval));
    }

    public static Vector3 CalculateWorldLungeOffset(
        Vector3 attackerWorldPosition,
        Vector3 targetWorldPosition,
        float distance = DefaultLungeDistance)
    {
        Vector3 delta = targetWorldPosition - attackerWorldPosition;
        delta.z = 0f;
        if (delta.sqrMagnitude <= 0.000001f) return Vector3.zero;
        return delta.normalized * Mathf.Max(0f, distance);
    }

    public static IEnumerator PlayDirectionalCast(
        GameObject attackerView,
        Vector2Int direction,
        float phaseDuration,
        Action onRelease)
    {
        float duration = Mathf.Max(0.01f, phaseDuration);
        float forwardSeconds = duration * ForwardRatio;
        float holdSeconds = duration * FlashHoldRatio;
        float returnSeconds = duration - forwardSeconds - holdSeconds;
        var lunges = new List<LungeState>();

        Transform visual = ResolveVisual(attackerView);
        if (visual != null && visual != attackerView.transform && !activeLungeVisuals.Contains(visual))
        {
            Vector3 worldOffset = new Vector3(direction.x, direction.y, 0f).normalized
                * DefaultLungeDistance;
            Vector3 localOffset = visual.parent != null
                ? visual.parent.InverseTransformVector(worldOffset)
                : worldOffset;
            activeLungeVisuals.Add(visual);
            lunges.Add(new LungeState
            {
                Visual = visual,
                BaseLocalPosition = visual.localPosition,
                LungeLocalOffset = localOffset,
            });
        }

        try
        {
            yield return TweenLunges(lunges, 0f, 1f, forwardSeconds);
            onRelease?.Invoke();
            yield return new WaitForSeconds(holdSeconds);
            yield return TweenLunges(lunges, 1f, 0f, returnSeconds);
        }
        finally
        {
            RestoreLunges(lunges);
        }
    }

    public static IEnumerator PlayAttack(
        IList<GameObject> attackerViews,
        GameObject targetView,
        float phaseDuration,
        Action onImpact)
    {
        float duration = Mathf.Max(0.01f, phaseDuration);
        float forwardSeconds = duration * ForwardRatio;
        float flashHoldSeconds = duration * FlashHoldRatio;
        float returnSeconds = duration - forwardSeconds - flashHoldSeconds;

        var lunges = new List<LungeState>();
        bool hasVisibleAttacker = CaptureLunges(attackerViews, targetView, lunges);
        List<SpriteRenderer> flashedRenderers = null;

        try
        {
            yield return TweenLunges(lunges, 0f, 1f, forwardSeconds);

            if (hasVisibleAttacker)
                flashedRenderers = BeginWhiteFlash(targetView);

            onImpact?.Invoke();
            yield return new WaitForSeconds(flashHoldSeconds);

            EndWhiteFlash(flashedRenderers);
            flashedRenderers = null;

            yield return TweenLunges(lunges, 1f, 0f, returnSeconds);
        }
        finally
        {
            EndWhiteFlash(flashedRenderers);
            RestoreLunges(lunges);
        }
    }

    private static bool CaptureLunges(
        IList<GameObject> attackerViews,
        GameObject targetView,
        List<LungeState> results)
    {
        if (attackerViews == null || targetView == null) return false;

        bool hasVisibleAttacker = false;
        for (int i = 0; i < attackerViews.Count; i++)
        {
            GameObject attackerView = attackerViews[i];
            if (attackerView == null) continue;
            hasVisibleAttacker = true;

            Transform visual = ResolveVisual(attackerView);
            if (visual == null || visual == attackerView.transform || activeLungeVisuals.Contains(visual))
                continue;

            Vector3 worldOffset = CalculateWorldLungeOffset(
                attackerView.transform.position,
                targetView.transform.position);
            if (worldOffset == Vector3.zero) continue;

            Vector3 localOffset = visual.parent != null
                ? visual.parent.InverseTransformVector(worldOffset)
                : worldOffset;

            activeLungeVisuals.Add(visual);
            results.Add(new LungeState
            {
                Visual = visual,
                BaseLocalPosition = visual.localPosition,
                LungeLocalOffset = localOffset,
            });
        }

        return hasVisibleAttacker;
    }

    private static Transform ResolveVisual(GameObject view)
    {
        if (view == null) return null;
        Transform namedVisual = view.transform.Find("Visual");
        if (namedVisual != null) return namedVisual;

        SpriteRenderer renderer = view.GetComponentInChildren<SpriteRenderer>(true);
        return renderer != null ? renderer.transform : null;
    }

    private static IEnumerator TweenLunges(
        IList<LungeState> lunges,
        float from,
        float to,
        float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, seconds));
            float eased = Mathf.SmoothStep(from, to, t);
            ApplyLungeWeight(lunges, eased);
            yield return null;
        }

        ApplyLungeWeight(lunges, to);
    }

    private static void ApplyLungeWeight(IList<LungeState> lunges, float weight)
    {
        for (int i = 0; i < lunges.Count; i++)
        {
            LungeState state = lunges[i];
            if (state.Visual == null) continue;
            state.Visual.localPosition = state.BaseLocalPosition + state.LungeLocalOffset * weight;
        }
    }

    private static void RestoreLunges(IList<LungeState> lunges)
    {
        for (int i = 0; i < lunges.Count; i++)
        {
            LungeState state = lunges[i];
            if (state.Visual != null)
                state.Visual.localPosition = state.BaseLocalPosition;
            activeLungeVisuals.Remove(state.Visual);
        }
    }

    private static List<SpriteRenderer> BeginWhiteFlash(GameObject targetView)
    {
        Material flashMaterial = GetWhiteFlashMaterial();
        if (targetView == null || flashMaterial == null) return null;

        SpriteRenderer[] renderers = targetView.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0) return null;

        var started = new List<SpriteRenderer>(renderers.Length);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null) continue;

            if (!activeFlashes.TryGetValue(renderer, out FlashState state))
            {
                state = new FlashState
                {
                    OriginalMaterial = renderer.sharedMaterial,
                    ReferenceCount = 0,
                };
                activeFlashes.Add(renderer, state);
                renderer.sharedMaterial = flashMaterial;
            }

            state.ReferenceCount++;
            started.Add(renderer);
        }

        return started;
    }

    private static void EndWhiteFlash(IList<SpriteRenderer> renderers)
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Count; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (ReferenceEquals(renderer, null)) continue;
            if (!activeFlashes.TryGetValue(renderer, out FlashState state)) continue;

            state.ReferenceCount--;
            if (state.ReferenceCount > 0) continue;

            if (renderer != null)
                renderer.sharedMaterial = state.OriginalMaterial;
            activeFlashes.Remove(renderer);
        }
    }

    private static Material GetWhiteFlashMaterial()
    {
        if (whiteFlashMaterialChecked) return whiteFlashMaterial;
        whiteFlashMaterialChecked = true;
        whiteFlashMaterial = Resources.Load<Material>(WhiteFlashMaterialPath);
        if (whiteFlashMaterial == null)
            Debug.LogWarning($"[CombatPresentation] Missing Resources/{WhiteFlashMaterialPath}; hit flash disabled.");
        return whiteFlashMaterial;
    }
}
