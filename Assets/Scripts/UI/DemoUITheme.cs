using UnityEngine;
using UnityEngine.UI;

public enum DemoUIButtonStyle
{
    Menu,
    Compact,
    Primary,
}

[System.Serializable]
public sealed class DemoUIButtonSet
{
    public Sprite normal;
    public Sprite hover;
    public Sprite pressed;
    public Sprite hoverPressed;

    public bool IsComplete => normal != null && hover != null
        && pressed != null && hoverPressed != null;

    public SpriteState CreateSpriteState()
    {
        return new SpriteState
        {
            highlightedSprite = hover,
            pressedSprite = pressed,
            selectedSprite = hoverPressed,
            disabledSprite = normal,
        };
    }
}

[CreateAssetMenu(fileName = "demo_ui_theme", menuName = "WhatDidIDo/UI/Demo Theme")]
public sealed class DemoUITheme : ScriptableObject
{
    public Font font;
    public Sprite menuPanel;
    public Sprite settingsPanel;
    public Sprite dropdownField;
    public DemoUIButtonSet menuButtons = new DemoUIButtonSet();
    public DemoUIButtonSet compactButtons = new DemoUIButtonSet();
    public DemoUIButtonSet primaryButtons = new DemoUIButtonSet();
    public Sprite playIcon;
    public Sprite homeIcon;
    public Sprite openListIcon;
    public Sprite soundIcon;
    public Sprite fullscreenIcon;
    public Sprite windowModeIcon;
    public Sprite meterSegmentActive;
    public Sprite meterSegmentInactive;

    public DemoUIButtonSet GetButtonSet(DemoUIButtonStyle style)
    {
        if (style == DemoUIButtonStyle.Compact) return compactButtons;
        if (style == DemoUIButtonStyle.Primary) return primaryButtons;
        return menuButtons;
    }

    public bool IsComplete => font != null
        && menuPanel != null && settingsPanel != null && dropdownField != null
        && menuButtons.IsComplete && compactButtons.IsComplete
        && primaryButtons.IsComplete && playIcon != null && homeIcon != null
        && openListIcon != null && soundIcon != null && fullscreenIcon != null
        && windowModeIcon != null && meterSegmentActive != null
        && meterSegmentInactive != null;
}
