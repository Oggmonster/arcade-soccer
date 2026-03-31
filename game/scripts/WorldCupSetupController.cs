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
        GetNode<Button>("%PrevButton").Pressed += () => CycleTeam(-1);
        GetNode<Button>("%NextButton").Pressed += () => CycleTeam(1);
        GetNode<Button>("%BackButton").Pressed += () => BackRequested?.Invoke();
        GetNode<Button>("%StartButton").Pressed += () => StartRequested?.Invoke(NationalTeamDatabase.Teams[_selectedIndex].Team.Name);
        RefreshUi();
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
