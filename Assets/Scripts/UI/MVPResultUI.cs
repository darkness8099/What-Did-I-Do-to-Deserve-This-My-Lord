using UnityEngine;
using UnityEngine.UI;

// Result overlay. Built entirely from Unity's built-in UI components because the project has no
// UI art yet (Assets/Art/UI is empty — see TASK-075). Swap the solid Images for sprites once
// ui_panel_result / ui_title_victory / ui_title_defeat exist.
//
// All copy is intentionally English: the built-in LegacyRuntime font carries no CJK glyphs.
public class MVPResultUI : MonoBehaviour
{
    private static readonly Color ColorBackdrop = new Color(0f, 0f, 0f, 0.72f);
    private static readonly Color ColorPanel    = new Color(0.08f, 0.09f, 0.12f, 0.96f);
    private static readonly Color ColorVictory  = new Color(1f, 0.84f, 0.32f);
    private static readonly Color ColorDefeat   = new Color(1f, 0.36f, 0.36f);
    private static readonly Color ColorHint     = new Color(0.75f, 0.78f, 0.82f);

    private MVPGameManager gameManager;
    private GameObject     overlayRoot;
    private Image          accentBar;
    private Text           titleText;
    private GameState      renderedState = GameState.Playing;

    private void Start()
    {
        gameManager = FindObjectOfType<MVPGameManager>();
        if (gameManager == null)
            Debug.LogError("[MVPResultUI] MVPGameManager not found.");

        BuildUI();
        Debug.Log("[MVPResultUI] Initialized.");
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("ResultCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        // Overlay avoids depending on Camera.main being present/ordered correctly.
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Full-screen dim; doubles as the on/off switch for the whole overlay.
        overlayRoot = CreateChild("Overlay", canvasGO.transform);
        var backdrop = overlayRoot.AddComponent<Image>();
        backdrop.color = ColorBackdrop;
        Stretch(overlayRoot.GetComponent<RectTransform>());

        var panel = CreateChild("Panel", overlayRoot.transform);
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = ColorPanel;
        SetBox(panel.GetComponent<RectTransform>(), new Vector2(900f, 420f), Vector2.zero);

        // Thin accent rule, tinted per outcome — gives the card structure without any art.
        var accent = CreateChild("AccentBar", panel.transform);
        accentBar = accent.AddComponent<Image>();
        SetBox(accent.GetComponent<RectTransform>(), new Vector2(360f, 6f), new Vector2(0f, -46f));

        titleText = CreateText("Title", panel.transform, 118, FontStyle.Bold, ColorVictory);
        SetBox(titleText.GetComponent<RectTransform>(), new Vector2(860f, 170f), new Vector2(0f, 62f));

        Text hint = CreateText("Hint", panel.transform, 34, FontStyle.Normal, ColorHint);
        hint.text = "Press  R  to Restart";
        SetBox(hint.GetComponent<RectTransform>(), new Vector2(860f, 60f), new Vector2(0f, -122f));

        overlayRoot.SetActive(false);
    }

    private static GameObject CreateChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style, Color color)
    {
        var go = CreateChild(name, parent);
        var text = go.AddComponent<Text>();
        text.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize  = fontSize;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color     = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow   = VerticalWrapMode.Overflow;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Centre-anchored fixed-size box, so layout holds at any resolution.
    private static void SetBox(RectTransform rect, Vector2 size, Vector2 offset)
    {
        rect.anchorMin        = new Vector2(0.5f, 0.5f);
        rect.anchorMax        = new Vector2(0.5f, 0.5f);
        rect.pivot            = new Vector2(0.5f, 0.5f);
        rect.sizeDelta        = size;
        rect.anchoredPosition = offset;
    }

    // Only repaints when the state actually changes (the old version rewrote text every frame).
    private void Update()
    {
        if (gameManager == null || overlayRoot == null) return;

        GameState state = gameManager.GetCurrentState();
        if (state == renderedState) return;
        renderedState = state;

        if (state == GameState.Playing)
        {
            overlayRoot.SetActive(false);
            return;
        }

        bool victory = state == GameState.Victory;
        titleText.text  = victory ? "VICTORY" : "DEFEAT";
        titleText.color = victory ? ColorVictory : ColorDefeat;
        accentBar.color = victory ? ColorVictory : ColorDefeat;
        overlayRoot.SetActive(true);
    }
}
