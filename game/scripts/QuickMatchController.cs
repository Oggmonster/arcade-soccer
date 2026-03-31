using System;
using System.Linq;
using Godot;

public partial class QuickMatchController : Control
{
    public event Action? BackRequested;
    public event Action<MatchSettings>? MatchRequested;

    private int _homeIndex;
    private int _awayIndex = 1;
    private MatchControlMode _controlMode = MatchControlMode.PlayerVsCpu;

    public override void _Ready()
    {
        GetNode<Button>("%HomePrevButton").Pressed += () => CycleTeam(ref _homeIndex, -1, true);
        GetNode<Button>("%HomeNextButton").Pressed += () => CycleTeam(ref _homeIndex, 1, true);
        GetNode<Button>("%AwayPrevButton").Pressed += () => CycleTeam(ref _awayIndex, -1, false);
        GetNode<Button>("%AwayNextButton").Pressed += () => CycleTeam(ref _awayIndex, 1, false);
        GetNode<Button>("%ModeButton").Pressed += ToggleMode;
        GetNode<Button>("%BackButton").Pressed += () => BackRequested?.Invoke();
        GetNode<Button>("%StartButton").Pressed += StartMatch;
        RefreshUi();
    }

    private void ToggleMode()
    {
        _controlMode = _controlMode == MatchControlMode.PlayerVsCpu
            ? MatchControlMode.PlayerVsPlayer
            : MatchControlMode.PlayerVsCpu;
        RefreshUi();
    }

    private void CycleTeam(ref int index, int delta, bool homeTeam)
    {
        var totalTeams = NationalTeamDatabase.Teams.Count;
        index = (index + delta + totalTeams) % totalTeams;
        if (_homeIndex == _awayIndex)
        {
            if (homeTeam)
            {
                _awayIndex = (_awayIndex + 1) % totalTeams;
            }
            else
            {
                _homeIndex = (_homeIndex + totalTeams - 1) % totalTeams;
            }
        }

        RefreshUi();
    }

    private void StartMatch()
    {
        var homeTeam = NationalTeamDatabase.Teams[_homeIndex].Team;
        var awayTeam = NationalTeamDatabase.Teams[_awayIndex].Team;
        var settings = new MatchSettings(
            homeTeam,
            awayTeam,
            true,
            _controlMode == MatchControlMode.PlayerVsPlayer,
            "Rematch",
            "Return to Menu",
            _controlMode == MatchControlMode.PlayerVsPlayer ? "Local Versus" : "Quick Match");
        MatchRequested?.Invoke(settings);
    }

    private void RefreshUi()
    {
        var home = NationalTeamDatabase.Teams[_homeIndex];
        var away = NationalTeamDatabase.Teams[_awayIndex];

        GetNode<Label>("%HomeTeamLabel").Text = home.Team.Name;
        GetNode<Label>("%AwayTeamLabel").Text = away.Team.Name;
        GetNode<RichTextLabel>("%HomeRosterLabel").Text = BuildRosterText(home);
        GetNode<RichTextLabel>("%AwayRosterLabel").Text = BuildRosterText(away);
        GetNode<Button>("%ModeButton").Text = _controlMode == MatchControlMode.PlayerVsCpu
            ? "Opponent: CPU"
            : "Opponent: Player 2";
        GetNode<Label>("%ControlsLabel").Text = _controlMode == MatchControlMode.PlayerVsCpu
            ? "Player 1: Arrows + J/K or Controller"
            : "Player 1: Arrows + J/K or Controller    Player 2: WASD + F/G";
    }

    private static string BuildRosterText(NationalTeamProfile profile)
    {
        var roles = profile.Team.Roles.Select(role => PocketPitchConfig.RoleName(role)).ToArray();
        return string.Join('\n', profile.PlayerNames.Select((name, index) => $"{name} - {roles[index]}"));
    }
}
