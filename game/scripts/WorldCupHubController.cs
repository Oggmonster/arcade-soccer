using System;
using Godot;

public partial class WorldCupHubController : Control
{
    public event Action? BackRequested;
    public event Action? PlayMatchRequested;

    public override void _Ready()
    {
        ArcadeUiStyler.ApplyTitle(GetNode<Label>("%TitleLabel"), new Color(1f, 0.90f, 0.49f, 1f), 38);
        ArcadeUiStyler.ApplySubtitle(GetNode<Label>("%StatusLabel"), new Color(0.84f, 0.94f, 1f, 1f), 19);
        ArcadeUiStyler.ApplyDataPanel(GetNode<PanelContainer>("Margin/VBox/Columns/GroupsPanel"), new Color(0.07f, 0.20f, 0.18f, 0.95f), new Color(0.39f, 0.96f, 0.81f, 1f));
        ArcadeUiStyler.ApplyDataPanel(GetNode<PanelContainer>("Margin/VBox/Columns/BracketPanel"), new Color(0.16f, 0.12f, 0.24f, 0.95f), new Color(0.98f, 0.68f, 0.27f, 1f));
        ArcadeUiStyler.ApplyRichText(GetNode<RichTextLabel>("%GroupsLabel"), new Color(0.91f, 1f, 0.96f, 1f), 18);
        ArcadeUiStyler.ApplyRichText(GetNode<RichTextLabel>("%BracketLabel"), new Color(0.98f, 0.94f, 1f, 1f), 18);
        ArcadeUiStyler.ApplySecondaryButton(GetNode<Button>("%BackButton"));
        ArcadeUiStyler.ApplyPrimaryButton(GetNode<Button>("%PlayButton"));
        GetNode<Button>("%BackButton").Pressed += () => BackRequested?.Invoke();
        GetNode<Button>("%PlayButton").Pressed += () => PlayMatchRequested?.Invoke();
    }

    public void Configure(WorldCupTournament tournament)
    {
        if (!IsNodeReady())
        {
            return;
        }

        GetNode<Label>("%TitleLabel").Text = $"{tournament.PlayerTeam.Team.Name} - {tournament.CurrentStageLabel}";
        GetNode<Label>("%StatusLabel").Text = tournament.StatusText;
        GetNode<RichTextLabel>("%GroupsLabel").Text = tournament.BuildGroupTableText();
        GetNode<RichTextLabel>("%BracketLabel").Text = tournament.BuildBracketText();
        var playButton = GetNode<Button>("%PlayButton");
        var backButton = GetNode<Button>("%BackButton");
        playButton.Disabled = !tournament.HasPlayableMatch;
        playButton.Text = tournament.HasPlayableMatch ? "Play Next Match" : "Tournament Complete";
        if (tournament.HasPlayableMatch)
        {
            playButton.GrabFocus();
        }
        else
        {
            backButton.GrabFocus();
        }
    }
}
