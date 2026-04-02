using System;
using System.Linq;
using Godot;

public partial class WorldCupSetupController : Control
{
    public event Action? BackRequested;
    public event Action<string>? StartRequested;

    private int _selectedIndex;

    public override void _Ready()
    {
        ArcadeUiStyler.ApplyHeroPanel(GetNode<PanelContainer>("Center/Panel"), new Color(0.10f, 0.13f, 0.24f, 0.96f), new Color(0.98f, 0.73f, 0.21f, 1f));
        ArcadeUiStyler.ApplyTitle(GetNode<Label>("Center/Panel/VBox/Title"), new Color(1f, 0.90f, 0.50f, 1f), 38);
        ArcadeUiStyler.ApplySubtitle(GetNode<Label>("Center/Panel/VBox/Subtitle"), new Color(0.86f, 0.93f, 1f, 1f), 18);
        ArcadeUiStyler.ApplyTinyButton(GetNode<Button>("%PrevButton"));
        ArcadeUiStyler.ApplyTinyButton(GetNode<Button>("%NextButton"));
        ArcadeUiStyler.ApplyValueLabel(GetNode<Label>("%TeamLabel"), new Color(0.74f, 0.96f, 1f, 1f), 30);
        ArcadeUiStyler.ApplySubtitle(GetNode<Label>("%GroupLabel"), new Color(1f, 0.86f, 0.57f, 1f), 22);
        ArcadeUiStyler.ApplyRichText(GetNode<RichTextLabel>("%RosterLabel"), new Color(0.93f, 0.97f, 1f, 1f), 19);
        ArcadeUiStyler.ApplySecondaryButton(GetNode<Button>("%BackButton"));
        ArcadeUiStyler.ApplyPrimaryButton(GetNode<Button>("%StartButton"));

        var prevButton = GetNode<Button>("%PrevButton");
        prevButton.Pressed += () => CycleTeam(-1);
        GetNode<Button>("%NextButton").Pressed += () => CycleTeam(1);
        GetNode<Button>("%BackButton").Pressed += () => BackRequested?.Invoke();
        GetNode<Button>("%StartButton").Pressed += () => StartRequested?.Invoke(NationalTeamDatabase.Teams[_selectedIndex].Team.Name);
        RefreshUi();
        prevButton.GrabFocus();
    }

    private void CycleTeam(int delta)
    {
        _selectedIndex = (_selectedIndex + delta + NationalTeamDatabase.Teams.Count) % NationalTeamDatabase.Teams.Count;
        RefreshUi();
    }

    private void RefreshUi()
    {
        var team = NationalTeamDatabase.Teams[_selectedIndex];
        GetNode<Label>("%TeamLabel").Text = team.Team.Name;
        GetNode<RichTextLabel>("%RosterLabel").Text = string.Join('\n',
            team.PlayerNames.Select((name, index) => $"{name} - {PocketPitchConfig.RoleName(team.Team.Roles[index])}"));
        GetNode<Label>("%GroupLabel").Text = $"Group: {FindGroup(team.Team.Name)}";
        ArcadeUiStyler.ApplyValueLabel(GetNode<Label>("%TeamLabel"), team.Team.PrimaryColor.Lightened(0.28f), 30);
    }

    private static string FindGroup(string teamName)
    {
        for (var i = 0; i < NationalTeamDatabase.WorldCupGroups.Count; i++)
        {
            if (NationalTeamDatabase.WorldCupGroups[i].Contains(teamName))
            {
                return $"{(char)('A' + i)}";
            }
        }

        return "?";
    }
}
