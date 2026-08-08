using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Runtime-built demo shell skinned by the Resources/UI demo theme.
public class MVPResultUI : MonoBehaviour
{
    private enum SettingsReturnTarget { MainMenu, Pause, Result }

    private static readonly Color Backdrop = new Color(0f, 0f, 0f, 0.78f);
    private static readonly Color MainBackdrop = new Color(0.018f, 0.022f, 0.03f, 1f);
    private static readonly Color Panel = new Color32(232, 214, 156, 255);
    private static readonly Color Gold = new Color32(58, 150, 88, 255);
    private static readonly Color Red = new Color32(150, 58, 48, 255);
    private static readonly Color TextMain = new Color32(66, 42, 28, 255);
    private static readonly Color TextMuted = new Color32(104, 69, 45, 255);
    private static readonly Color ButtonNormal = new Color(0.14f, 0.17f, 0.22f, 1f);
    private static readonly Color ButtonHover = new Color(0.23f, 0.28f, 0.36f, 1f);
    private static DemoUITheme theme;

    private MVPGameManager gameManager;
    private GameObject canvasRoot;
    private GameObject menuButtonRoot;
    private GameObject mainMenuRoot;
    private GameObject pauseRoot;
    private GameObject resultRoot;
    private GameObject settingsRoot;
    private Text resultTitle;
    private Text resultSubtitle;
    private Image resultAccent;
    private Text volumeLabel;
    private Text fullscreenLabel;
    private Text resolutionLabel;
    private Slider volumeSlider;
    private readonly System.Collections.Generic.List<Image> volumeMeterSegments =
        new System.Collections.Generic.List<Image>();
    private GameState renderedState = GameState.Playing;
    private SettingsReturnTarget settingsReturnTarget;

    public bool IsMainMenuVisible => mainMenuRoot != null && mainMenuRoot.activeSelf;
    public bool IsPauseMenuVisible => pauseRoot != null && pauseRoot.activeSelf;
    public bool IsSettingsVisible => settingsRoot != null && settingsRoot.activeSelf;
    public bool IsResultVisible => resultRoot != null && resultRoot.activeSelf;
    public int RuntimeButtonCount => canvasRoot != null
        ? canvasRoot.GetComponentsInChildren<Button>(true).Length
        : 0;
    public int RuntimeSliderCount => canvasRoot != null
        ? canvasRoot.GetComponentsInChildren<Slider>(true).Length
        : 0;
    public bool HasCompleteTheme => theme != null && theme.IsComplete;
    public int VolumeMeterSegmentCount => volumeMeterSegments.Count;

    private void Start()
    {
        InitializeUI();
    }

    public void InitializeUI(MVPGameManager managerOverride = null)
    {
        if (canvasRoot != null) return;

        gameManager = managerOverride != null
            ? managerOverride
            : FindObjectOfType<MVPGameManager>();
        if (gameManager == null)
            Debug.LogError("[MVPResultUI] MVPGameManager not found.");

        theme = Resources.Load<DemoUITheme>("UI/demo_ui_theme");
        if (theme == null || !theme.IsComplete)
            Debug.LogError("[MVPResultUI] Resources/UI/demo_ui_theme is missing or incomplete.");

        DemoGameFlow.InitializeSettings();
        EnsureEventSystem();
        BuildUI();

        if (DemoGameFlow.LaunchMode == DemoLaunchMode.MainMenu)
            ShowMainMenu();
        else
            EnterGameplaySurface();

        DemoBgmDirector bgmDirector = GetComponent<DemoBgmDirector>();
        if (bgmDirector == null) bgmDirector = gameObject.AddComponent<DemoBgmDirector>();
        bgmDirector.Initialize(this);

        Debug.Log("[MVPResultUI] Demo UI flow initialized.");
    }

    private void Update()
    {
        if (canvasRoot == null) return;

        if (gameManager != null)
        {
            GameState state = gameManager.GetCurrentState();
            if (state != renderedState)
            {
                renderedState = state;
                if (state != GameState.Playing)
                    ShowResult(state);
            }
        }

        bool escapePressed = Input.GetKeyDown(KeyCode.Escape);
        if (escapePressed && IsSettingsVisible)
        {
            CloseSettings();
            return;
        }

        if (renderedState != GameState.Playing
            || DemoGameFlow.LaunchMode != DemoLaunchMode.Gameplay
            || !escapePressed)
            return;

        if (IsPauseMenuVisible)
            ResumeGame();
        else
            PauseGame();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    public void ShowMainMenu()
    {
        DemoGameFlow.MarkMainMenu();
        Time.timeScale = 0f;
        renderedState = GameState.Playing;
        SetOnly(mainMenuRoot);
        if (menuButtonRoot != null) menuButtonRoot.SetActive(false);
    }

    public void StartGame()
    {
        DemoGameFlow.EnterGameplay();
        EnterGameplaySurface();
    }

    public void PauseGame()
    {
        if (renderedState != GameState.Playing
            || DemoGameFlow.LaunchMode != DemoLaunchMode.Gameplay)
            return;

        Time.timeScale = 0f;
        SetOnly(pauseRoot);
        menuButtonRoot.SetActive(false);
    }

    public void ResumeGame()
    {
        if (renderedState != GameState.Playing) return;
        Time.timeScale = 1f;
        SetOnly(null);
        menuButtonRoot.SetActive(true);
    }

    public void ShowResult(GameState state)
    {
        if (state == GameState.Playing) return;

        renderedState = state;
        bool victory = state == GameState.Victory;
        resultTitle.text = victory ? "VICTORY" : "DEFEAT";
        resultTitle.color = victory ? Gold : Red;
        resultAccent.color = victory ? Gold : Red;
        resultSubtitle.text = victory
            ? "ALL HERO WAVES HAVE BEEN DEFEATED"
            : "THE DEMON LORD WAS TAKEN TO THE ENTRANCE";
        Time.timeScale = 0f;
        SetOnly(resultRoot);
        menuButtonRoot.SetActive(false);
    }

    public void OpenSettingsFromMainMenu()
    {
        OpenSettings(SettingsReturnTarget.MainMenu);
    }

    public void OpenSettingsFromPause()
    {
        OpenSettings(SettingsReturnTarget.Pause);
    }

    public void OpenSettingsFromResult()
    {
        OpenSettings(SettingsReturnTarget.Result);
    }

    public void CloseSettings()
    {
        settingsRoot.SetActive(false);
        if (settingsReturnTarget == SettingsReturnTarget.MainMenu)
            mainMenuRoot.SetActive(true);
        else if (settingsReturnTarget == SettingsReturnTarget.Pause)
            pauseRoot.SetActive(true);
        else
            resultRoot.SetActive(true);
    }

    private void EnterGameplaySurface()
    {
        Time.timeScale = 1f;
        SetOnly(null);
        menuButtonRoot.SetActive(true);
    }

    private void OpenSettings(SettingsReturnTarget returnTarget)
    {
        settingsReturnTarget = returnTarget;
        RefreshSettingsLabels();
        settingsRoot.SetActive(true);
        menuButtonRoot.SetActive(false);
    }

    private void SetOnly(GameObject visibleRoot)
    {
        if (mainMenuRoot != null) mainMenuRoot.SetActive(mainMenuRoot == visibleRoot);
        if (pauseRoot != null) pauseRoot.SetActive(pauseRoot == visibleRoot);
        if (resultRoot != null) resultRoot.SetActive(resultRoot == visibleRoot);
        if (settingsRoot != null) settingsRoot.SetActive(settingsRoot == visibleRoot);
    }

    private void BuildUI()
    {
        canvasRoot = new GameObject("DemoUICanvas");
        Canvas canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        canvas.pixelPerfect = true;
        CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRoot.AddComponent<GraphicRaycaster>();

        BuildMenuButton();
        BuildMainMenu();
        BuildPauseMenu();
        BuildResultMenu();
        BuildSettingsMenu();

        mainMenuRoot.SetActive(false);
        pauseRoot.SetActive(false);
        resultRoot.SetActive(false);
        settingsRoot.SetActive(false);
    }

    private void BuildMenuButton()
    {
        Button button = CreateButton("MenuButton", canvasRoot.transform, "MENU", PauseGame,
            DemoUIButtonStyle.Primary);
        menuButtonRoot = button.gameObject;
        SetAnchoredBox(button.GetComponent<RectTransform>(), new Vector2(168f, 64f),
            new Vector2(-42f, -36f), new Vector2(1f, 1f));
    }

    private void BuildMainMenu()
    {
        mainMenuRoot = CreateScreen("MainMenu", MainBackdrop);
        Vector2 panelSize = new Vector2(552f, 1008f);
        GameObject panel = CreatePanel(mainMenuRoot.transform, panelSize,
            theme != null ? theme.menuPanel : null);
        CreateCloseHotspot(panel.transform, panelSize, DemoGameFlow.QuitApplication);
        Text title = CreateLabel(panel.transform,
            "WHAT DID I DO TO\nDESERVE THIS, MY LORD", 32, FontStyle.Bold, TextMain,
            new Vector2(500f, 140f), new Vector2(0f, 300f));
        title.resizeTextForBestFit = true;
        title.resizeTextMinSize = 22;
        title.resizeTextMaxSize = 32;
        title.lineSpacing = 0.9f;
        CreateRule(panel.transform, Gold, new Vector2(180f, 5f), new Vector2(0f, 195f));

        CreateMenuButton(panel.transform, "START GAME", StartGame, 55f);
        CreateMenuButton(panel.transform, "SETTINGS", OpenSettingsFromMainMenu, -55f);
        CreateMenuButton(panel.transform, "QUIT", DemoGameFlow.QuitApplication, -165f);
        CreateLabel(panel.transform, "MINIMUM PLAYABLE DEMO / VERSION 0.1.0", 15,
            FontStyle.Normal, TextMuted, new Vector2(500f, 42f), new Vector2(0f, -365f));
    }

    private void BuildPauseMenu()
    {
        pauseRoot = CreateScreen("PauseMenu", Backdrop);
        Vector2 panelSize = new Vector2(480f, 1008f);
        GameObject panel = CreatePanel(pauseRoot.transform, panelSize,
            theme != null ? theme.menuPanel : null);
        CreateCloseHotspot(panel.transform, panelSize, ResumeGame);
        CreateLabel(panel.transform, "PAUSED", 46, FontStyle.Bold, TextMain,
            new Vector2(400f, 90f), new Vector2(0f, 300f));
        CreateRule(panel.transform, Gold, new Vector2(150f, 5f), new Vector2(0f, 240f));
        CreateMenuButton(panel.transform, "RESUME", ResumeGame, 110f);
        CreateMenuButton(panel.transform, "SETTINGS", OpenSettingsFromPause, 0f);
        CreateMenuButton(panel.transform, "RESTART", DemoGameFlow.ReloadGameplay, -110f);
        CreateMenuButton(panel.transform, "MAIN MENU", DemoGameFlow.ReturnToMainMenu, -220f);
    }

    private void BuildResultMenu()
    {
        resultRoot = CreateScreen("ResultMenu", Backdrop);
        Vector2 panelSize = new Vector2(480f, 1008f);
        GameObject panel = CreatePanel(resultRoot.transform, panelSize,
            theme != null ? theme.menuPanel : null);
        CreateCloseHotspot(panel.transform, panelSize, DemoGameFlow.ReturnToMainMenu);
        resultTitle = CreateLabel(panel.transform, "VICTORY", 50, FontStyle.Bold, Gold,
            new Vector2(410f, 100f), new Vector2(0f, 300f));
        resultAccent = CreateRule(panel.transform, Gold, new Vector2(190f, 6f), new Vector2(0f, 235f));
        resultSubtitle = CreateLabel(panel.transform, string.Empty, 18, FontStyle.Normal, TextMuted,
            new Vector2(400f, 72f), new Vector2(0f, 175f));
        CreateMenuButton(panel.transform, "RETRY", DemoGameFlow.ReloadGameplay, 35f);
        CreateMenuButton(panel.transform, "SETTINGS", OpenSettingsFromResult, -75f);
        CreateMenuButton(panel.transform, "MAIN MENU", DemoGameFlow.ReturnToMainMenu, -185f);
        CreateLabel(panel.transform, "PRESS R TO RETRY", 16, FontStyle.Normal, TextMuted,
            new Vector2(400f, 40f), new Vector2(0f, -365f));
    }

    private void BuildSettingsMenu()
    {
        settingsRoot = CreateScreen("SettingsMenu", Backdrop);
        Vector2 panelSize = new Vector2(588f, 894f);
        GameObject panel = CreatePanel(settingsRoot.transform, panelSize,
            theme != null ? theme.settingsPanel : null);
        CreateCloseHotspot(panel.transform, panelSize, CloseSettings);
        CreateLabel(panel.transform, "SETTINGS", 28, FontStyle.Bold, TextMain,
            new Vector2(390f, 54f), new Vector2(0f, 405f));

        CreateIcon(panel.transform, "SoundIcon", theme != null ? theme.soundIcon : null,
            new Vector2(48f, 48f), new Vector2(-220f, 284f));
        volumeLabel = CreateLabel(panel.transform, string.Empty, 22, FontStyle.Bold, TextMain,
            new Vector2(410f, 48f), new Vector2(28f, 284f));
        volumeSlider = CreateSlider(panel.transform, new Vector2(420f, 72f), new Vector2(0f, 215f));
        volumeSlider.SetValueWithoutNotify(DemoGameFlow.MasterVolume);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        Button fullscreen = CreateMenuButton(panel.transform, string.Empty,
            OnFullscreenClicked, 88f, DemoUIButtonStyle.Compact);
        fullscreenLabel = fullscreen.GetComponentInChildren<Text>();
        Button resolution = CreateMenuButton(panel.transform, string.Empty,
            OnResolutionClicked, -20f, DemoUIButtonStyle.Compact);
        resolutionLabel = resolution.GetComponentInChildren<Text>();
        CreateMenuButton(panel.transform, "RESET DEFAULTS", OnResetSettingsClicked, -128f,
            DemoUIButtonStyle.Compact);
        CreateMenuButton(panel.transform, "BACK", CloseSettings, -236f,
            DemoUIButtonStyle.Compact);

        CreateLabel(panel.transform, "DISPLAY OPTIONS USE 16:9 PRESETS", 15,
            FontStyle.Normal, TextMuted, new Vector2(500f, 38f), new Vector2(0f, -365f));
        RefreshSettingsLabels();
    }

    private void OnVolumeChanged(float value)
    {
        DemoGameFlow.SetMasterVolume(value);
        RefreshSettingsLabels();
    }

    private void OnFullscreenClicked()
    {
        DemoGameFlow.ToggleFullscreen();
        RefreshSettingsLabels();
    }

    private void OnResolutionClicked()
    {
        DemoGameFlow.CycleResolution();
        RefreshSettingsLabels();
    }

    private void OnResetSettingsClicked()
    {
        DemoGameFlow.ResetSettings();
        volumeSlider.SetValueWithoutNotify(DemoGameFlow.MasterVolume);
        RefreshSettingsLabels();
    }

    private void RefreshSettingsLabels()
    {
        if (volumeLabel != null)
            volumeLabel.text = $"VOLUME {Mathf.RoundToInt(DemoGameFlow.MasterVolume * 100f)}%";
        if (fullscreenLabel != null)
            fullscreenLabel.text = DemoGameFlow.GetFullscreenLabel();
        if (resolutionLabel != null)
            resolutionLabel.text = DemoGameFlow.GetResolutionLabel();

        int activeSegments = Mathf.RoundToInt(
            DemoGameFlow.MasterVolume * volumeMeterSegments.Count);
        for (int i = 0; i < volumeMeterSegments.Count; i++)
        {
            volumeMeterSegments[i].sprite = theme != null && i < activeSegments
                ? theme.meterSegmentActive
                : theme != null ? theme.meterSegmentInactive : null;
        }
    }

    private GameObject CreateScreen(string name, Color color)
    {
        GameObject root = CreateChild(name, canvasRoot.transform);
        Image image = root.AddComponent<Image>();
        image.color = color;
        Stretch(root.GetComponent<RectTransform>());
        return root;
    }

    private static GameObject CreatePanel(Transform parent, Vector2 size, Sprite sprite)
    {
        GameObject panel = CreateChild("Panel", parent);
        Image image = panel.AddComponent<Image>();
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : Panel;
        image.type = Image.Type.Simple;
        SetBox(panel.GetComponent<RectTransform>(), size, Vector2.zero);
        return panel;
    }

    private static Button CreateMenuButton(Transform parent, string label,
        UnityEngine.Events.UnityAction action, float y,
        DemoUIButtonStyle style = DemoUIButtonStyle.Menu)
    {
        Button button = CreateButton(label.Replace(" ", string.Empty) + "Button",
            parent, label, action, style);
        Vector2 size = style == DemoUIButtonStyle.Menu
            ? new Vector2(336f, 72f)
            : new Vector2(420f, 78f);
        SetBox(button.GetComponent<RectTransform>(), size, new Vector2(0f, y));
        return button;
    }

    private static Button CreateButton(string name, Transform parent, string label,
        UnityEngine.Events.UnityAction action,
        DemoUIButtonStyle style = DemoUIButtonStyle.Menu)
    {
        GameObject root = CreateChild(name, parent);
        Image image = root.AddComponent<Image>();
        Button button = root.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        DemoUIButtonSet set = theme != null ? theme.GetButtonSet(style) : null;
        if (set != null && set.normal != null)
        {
            image.sprite = set.normal;
            image.color = Color.white;
            image.type = Image.Type.Sliced;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = set.CreateSpriteState();
        }
        else
        {
            image.color = ButtonNormal;
            ColorBlock colors = button.colors;
            colors.normalColor = ButtonNormal;
            colors.highlightedColor = ButtonHover;
            colors.pressedColor = Gold;
            colors.selectedColor = ButtonHover;
            colors.disabledColor = new Color(0.08f, 0.09f, 0.11f, 0.7f);
            button.colors = colors;
        }
        if (action != null) button.onClick.AddListener(action);

        int fontSize = style == DemoUIButtonStyle.Menu ? 25 : 20;
        Text text = CreateLabel(root.transform, label, fontSize, FontStyle.Bold, TextMain,
            Vector2.zero, Vector2.zero);
        StretchWithOffsets(text.rectTransform, 12f, 12f, 8f, 8f);
        return button;
    }

    private Slider CreateSlider(Transform parent, Vector2 size, Vector2 offset)
    {
        volumeMeterSegments.Clear();
        GameObject root = CreateChild("VolumeSlider", parent);
        SetBox(root.GetComponent<RectTransform>(), size, offset);
        Image backgroundImage = root.AddComponent<Image>();
        backgroundImage.sprite = theme != null ? theme.dropdownField : null;
        backgroundImage.color = backgroundImage.sprite != null ? Color.white : ButtonNormal;
        backgroundImage.type = backgroundImage.sprite != null
            ? Image.Type.Sliced
            : Image.Type.Simple;
        Slider slider = root.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;

        const int segmentCount = 16;
        const float spacing = 20f;
        float start = -0.5f * spacing * (segmentCount - 1);
        for (int i = 0; i < segmentCount; i++)
        {
            GameObject segment = CreateChild("Segment" + i, root.transform);
            Image segmentImage = segment.AddComponent<Image>();
            segmentImage.sprite = theme != null ? theme.meterSegmentInactive : null;
            segmentImage.color = segmentImage.sprite != null ? Color.white : TextMuted;
            segmentImage.preserveAspect = true;
            segmentImage.raycastTarget = false;
            SetBox(segment.GetComponent<RectTransform>(), new Vector2(12f, 48f),
                new Vector2(start + spacing * i, 0f));
            volumeMeterSegments.Add(segmentImage);
        }

        GameObject handleArea = CreateChild("HandleArea", root.transform);
        StretchWithOffsets(handleArea.GetComponent<RectTransform>(), 30f, 30f, 0f, 0f);
        GameObject handle = CreateChild("Handle", handleArea.transform);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.sprite = theme != null ? theme.meterSegmentActive : null;
        handleImage.color = handleImage.sprite != null ? Color.white : TextMain;
        handleImage.preserveAspect = true;
        handleImage.raycastTarget = false;
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(18f, 58f);
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        return slider;
    }

    private static Text CreateLabel(Transform parent, string value, int fontSize,
        FontStyle style, Color color, Vector2 size, Vector2 offset)
    {
        GameObject root = CreateChild("Label", parent);
        Text text = root.AddComponent<Text>();
        text.font = theme != null && theme.font != null
            ? theme.font
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        if (size != Vector2.zero)
            SetBox(text.rectTransform, size, offset);
        return text;
    }

    private static Image CreateIcon(Transform parent, string name, Sprite sprite,
        Vector2 size, Vector2 offset, Vector2? anchor = null)
    {
        GameObject root = CreateChild(name, parent);
        Image image = root.AddComponent<Image>();
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : Color.clear;
        image.preserveAspect = true;
        image.raycastTarget = false;
        SetAnchoredBox(root.GetComponent<RectTransform>(), size, offset,
            anchor ?? new Vector2(0.5f, 0.5f));
        return image;
    }

    private static Button CreateCloseHotspot(Transform parent, Vector2 panelSize,
        UnityEngine.Events.UnityAction action)
    {
        GameObject root = CreateChild("CloseHotspot", parent);
        Image image = root.AddComponent<Image>();
        image.color = Color.clear;
        Button button = root.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        if (action != null) button.onClick.AddListener(action);
        SetBox(root.GetComponent<RectTransform>(), new Vector2(72f, 72f),
            new Vector2(panelSize.x * 0.5f - 36f, panelSize.y * 0.5f - 42f));
        return button;
    }

    private static Image CreateRule(Transform parent, Color color, Vector2 size, Vector2 offset)
    {
        GameObject root = CreateChild("AccentRule", parent);
        Image image = root.AddComponent<Image>();
        image.color = color;
        SetBox(root.GetComponent<RectTransform>(), size, offset);
        return image;
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        GameObject root = new GameObject("DemoUIEventSystem");
        root.AddComponent<EventSystem>();
        root.AddComponent<StandaloneInputModule>();
    }

    private static GameObject CreateChild(string name, Transform parent)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        return root;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void StretchWithOffsets(RectTransform rect,
        float left, float right, float bottom, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void SetBox(RectTransform rect, Vector2 size, Vector2 offset)
    {
        SetAnchoredBox(rect, size, offset, new Vector2(0.5f, 0.5f));
    }

    private static void SetAnchoredBox(RectTransform rect, Vector2 size,
        Vector2 offset, Vector2 anchor)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = offset;
    }
}
