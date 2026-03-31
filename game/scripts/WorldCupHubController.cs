using System;
using Godot;

public partial class WorldCupHubController : Control
{
    public event Action? BackRequested;
    public event Action? PlayMatchRequested;

    public override void _Ready()
    {
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
        playButton.Disabled = !tournament.HasPlayableMatch;
        playButton.Text = tournament.HasPlayableMatch ? "Play Next Match" : "Tournament Complete";
    }
}
