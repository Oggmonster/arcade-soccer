using Godot;

public static class ArcadeUiStyler
{
    private static readonly Color Ink = new(0.05f, 0.08f, 0.14f, 1f);
    private static readonly Color SoftInk = new(0.10f, 0.16f, 0.22f, 1f);
    private static readonly Color Paper = new(0.95f, 0.98f, 1f, 1f);
    private static readonly Color Muted = new(0.71f, 0.82f, 0.91f, 1f);

    public static void ApplyHeroPanel(PanelContainer panel, Color bg, Color border)
    {
        panel.AddThemeStyleboxOverride("panel", BuildBox(bg, border, 26, 4, 26, 22));
    }

    public static void ApplyDataPanel(PanelContainer panel, Color bg, Color border)
    {
        panel.AddThemeStyleboxOverride("panel", BuildBox(bg, border, 20, 3, 18, 16));
    }

    public static void ApplyTitle(Label label, Color color, int fontSize)
    {
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", Ink);
        label.AddThemeConstantOverride("outline_size", 3);
        label.AddThemeFontSizeOverride("font_size", fontSize);
    }

    public static void ApplySubtitle(Label label, Color color, int fontSize = 18)
    {
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", SoftInk);
        label.AddThemeConstantOverride("outline_size", 1);
        label.AddThemeFontSizeOverride("font_size", fontSize);
    }

    public static void ApplyBodyLabel(Label label, Color color, int fontSize = 18)
    {
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", fontSize);
    }

    public static void ApplyValueLabel(Label label, Color color, int fontSize = 26)
    {
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", Ink);
        label.AddThemeConstantOverride("outline_size", 2);
        label.AddThemeFontSizeOverride("font_size", fontSize);
    }

    public static void ApplyRichText(RichTextLabel label, Color color, int fontSize = 18)
    {
        label.Modulate = color;
        label.AddThemeFontSizeOverride("normal_font_size", fontSize);
    }

    public static void ApplyPrimaryButton(Button button)
    {
        ApplyButton(button, new Color(0.96f, 0.45f, 0.17f, 1f), new Color(1f, 0.84f, 0.40f, 1f), Paper, 22);
    }

    public static void ApplyAccentButton(Button button)
    {
        ApplyButton(button, new Color(0.17f, 0.73f, 0.83f, 1f), new Color(0.60f, 0.98f, 1f, 1f), Paper, 20);
    }

    public static void ApplySecondaryButton(Button button)
    {
        ApplyButton(button, new Color(0.17f, 0.23f, 0.34f, 1f), new Color(0.48f, 0.84f, 0.93f, 1f), Paper, 20);
    }

    public static void ApplyTinyButton(Button button)
    {
        ApplyButton(button, new Color(0.17f, 0.23f, 0.34f, 1f), new Color(0.95f, 0.73f, 0.22f, 1f), Paper, 22, 14, 10);
    }

    public static void ApplyScoreChip(PanelContainer panel, Label label, Color bg, Color border, int fontSize)
    {
        ApplyDataPanel(panel, bg, border);
        label.AddThemeColorOverride("font_color", Paper);
        label.AddThemeColorOverride("font_outline_color", Ink);
        label.AddThemeConstantOverride("outline_size", 2);
        label.AddThemeFontSizeOverride("font_size", fontSize);
    }

    public static void ApplyStatusPanel(PanelContainer panel, Label label)
    {
        panel.AddThemeStyleboxOverride("panel", BuildBox(new Color(0.08f, 0.14f, 0.20f, 0.92f), new Color(1f, 0.84f, 0.40f, 1f), 18, 3, 20, 10));
        label.AddThemeColorOverride("font_color", Paper);
        label.AddThemeColorOverride("font_outline_color", Ink);
        label.AddThemeConstantOverride("outline_size", 1);
        label.AddThemeFontSizeOverride("font_size", 20);
    }

    public static void ApplyOverlayPanel(PanelContainer panel, Label titleLabel, Label bodyLabel)
    {
        panel.AddThemeStyleboxOverride("panel", BuildBox(new Color(0.07f, 0.12f, 0.18f, 0.97f), new Color(1f, 0.64f, 0.18f, 1f), 28, 4, 28, 24));
        ApplyTitle(titleLabel, new Color(1f, 0.90f, 0.55f, 1f), 36);
        ApplySubtitle(bodyLabel, Paper, 24);
    }

    private static void ApplyButton(Button button, Color bg, Color border, Color fontColor, int fontSize, int paddingX = 20, int paddingY = 12)
    {
        button.AddThemeStyleboxOverride("normal", BuildBox(bg, border, 20, 3, paddingX, paddingY));
        button.AddThemeStyleboxOverride("hover", BuildBox(bg.Lightened(0.08f), border.Lightened(0.12f), 20, 3, paddingX, paddingY));
        button.AddThemeStyleboxOverride("pressed", BuildBox(bg.Darkened(0.12f), border.Lightened(0.18f), 20, 3, paddingX, paddingY + 1));
        button.AddThemeStyleboxOverride("focus", BuildBox(bg.Lightened(0.03f), new Color(1f, 0.97f, 0.82f, 1f), 20, 4, paddingX, paddingY));
        button.AddThemeStyleboxOverride("disabled", BuildBox(bg.Darkened(0.35f), border.Darkened(0.2f), 20, 2, paddingX, paddingY));

        button.AddThemeColorOverride("font_color", fontColor);
        button.AddThemeColorOverride("font_hover_color", fontColor);
        button.AddThemeColorOverride("font_pressed_color", fontColor);
        button.AddThemeColorOverride("font_focus_color", fontColor);
        button.AddThemeColorOverride("font_disabled_color", Muted);
        button.AddThemeColorOverride("font_outline_color", Ink);
        button.AddThemeConstantOverride("outline_size", 2);
        button.AddThemeFontSizeOverride("font_size", fontSize);
    }

    private static StyleBoxFlat BuildBox(Color bg, Color border, int radius, int borderWidth, int paddingX, int paddingY)
    {
        var style = new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = border,
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomRight = radius,
            CornerRadiusBottomLeft = radius,
            ContentMarginLeft = paddingX,
            ContentMarginTop = paddingY,
            ContentMarginRight = paddingX,
            ContentMarginBottom = paddingY,
            ShadowColor = new Color(0f, 0f, 0f, 0.28f),
            ShadowSize = 8
        };

        return style;
    }
}
