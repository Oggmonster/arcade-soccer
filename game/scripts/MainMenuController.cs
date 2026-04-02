using System;
using Godot;

public partial class MainMenuController : Control
{
    public event Action? QuickMatchRequested;
    public event Action? WorldCupRequested;
    public event Action? QuitRequested;

    public override void _Ready()
    {
        ArcadeUiStyler.ApplyHeroPanel(GetNode<PanelContainer>("Center/Panel"), new Color(0.08f, 0.15f, 0.23f, 0.96f), new Color(0.97f, 0.69f, 0.20f, 1f));
        ArcadeUiStyler.ApplyTitle(GetNode<Label>("Center/Panel/VBox/Title"), new Color(0.99f, 0.89f, 0.46f, 1f), 54);
        ArcadeUiStyler.ApplySubtitle(GetNode<Label>("Center/Panel/VBox/Subtitle"), new Color(0.76f, 0.92f, 1f, 1f), 21);
        ArcadeUiStyler.ApplyBodyLabel(GetNode<Label>("Center/Panel/VBox/Hint"), new Color(0.83f, 0.92f, 1f, 1f), 17);

        var quickMatchButton = GetNode<Button>("%QuickMatchButton");
        ArcadeUiStyler.ApplyPrimaryButton(quickMatchButton);
        ArcadeUiStyler.ApplyAccentButton(GetNode<Button>("%WorldCupButton"));
        ArcadeUiStyler.ApplySecondaryButton(GetNode<Button>("%QuitButton"));
        quickMatchButton.Pressed += () => QuickMatchRequested?.Invoke();
        GetNode<Button>("%WorldCupButton").Pressed += () => WorldCupRequested?.Invoke();
        GetNode<Button>("%QuitButton").Pressed += () => QuitRequested?.Invoke();
        quickMatchButton.GrabFocus();
    }
}
