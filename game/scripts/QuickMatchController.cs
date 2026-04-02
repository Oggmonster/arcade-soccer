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
        ArcadeUiStyler.ApplyTitle(GetNode<Label>("Margin/VBox/Title"), new Color(0.98f, 0.91f, 0.49f, 1f), 40);
        ArcadeUiStyler.ApplySubtitle(GetNode<Label>("%ControlsLabel"), new Color(0.83f, 0.92f, 1f, 1f), 18);
        ArcadeUiStyler.ApplyAccentButton(GetNode<Button>("%ModeButton"));
        ArcadeUiStyler.ApplyHeroPanel(GetNode<PanelContainer>("Margin/VBox/Columns/HomePanel"), new Color(0.06f, 0.18f, 0.27f, 0.94f), new Color(0.36f, 0.90f, 1f, 1f));
        ArcadeUiStyler.ApplyHeroPanel(GetNode<PanelContainer>("Margin/VBox/Columns/AwayPanel"), new Color(0.25f, 0.12f, 0.08f, 0.94f), new Color(1f, 0.63f, 0.23f, 1f));
        ArcadeUiStyler.ApplySubtitle(GetNode<Label>("Margin/VBox/Columns/HomePanel/HomeVBox/HomeTitle"), new Color(0.76f, 0.95f, 1f, 1f), 24);
        ArcadeUiStyler.ApplySubtitle(GetNode<Label>("Margin/VBox/Columns/AwayPanel/AwayVBox/AwayTitle"), new Color(1f, 0.86f, 0.63f, 1f), 24);
        ArcadeUiStyler.ApplyTinyButton(GetNode<Button>("%HomePrevButton"));
        ArcadeUiStyler.ApplyTinyButton(GetNode<Button>("%HomeNextButton"));
        ArcadeUiStyler.ApplyTinyButton(GetNode<Button>("%AwayPrevButton"));
        ArcadeUiStyler.ApplyTinyButton(GetNode<Button>("%AwayNextButton"));
        ArcadeUiStyler.ApplySecondaryButton(GetNode<Button>("%BackButton"));
        ArcadeUiStyler.ApplyPrimaryButton(GetNode<Button>("%StartButton"));
        ArcadeUiStyler.ApplyRichText(GetNode<RichTextLabel>("%HomeRosterLabel"), new Color(0.90f, 0.97f, 1f, 1f), 18);
        ArcadeUiStyler.ApplyRichText(GetNode<RichTextLabel>("%AwayRosterLabel"), new Color(1f, 0.95f, 0.90f, 1f), 18);

        var modeButton = GetNode<Button>("%ModeButton");
        GetNode<Button>("%HomePrevButton").Pressed += () => CycleTeam(ref _homeIndex, -1, true);
        GetNode<Button>("%HomeNextButton").Pressed += () => CycleTeam(ref _homeIndex, 1, true);
        GetNode<Button>("%AwayPrevButton").Pressed += () => CycleTeam(ref _awayIndex, -1, false);
        GetNode<Button>("%AwayNextButton").Pressed += () => CycleTeam(ref _awayIndex, 1, false);
        modeButton.Pressed += ToggleMode;
        GetNode<Button>("%BackButton").Pressed += () => BackRequested?.Invoke();
        GetNode<Button>("%StartButton").Pressed += StartMatch;
        RefreshUi();
        modeButton.GrabFocus();
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
        var homeTeamLabel = GetNode<Label>("%HomeTeamLabel");
        var awayTeamLabel = GetNode<Label>("%AwayTeamLabel");

        homeTeamLabel.Text = home.Team.Name;
        awayTeamLabel.Text = away.Team.Name;
        GetNode<RichTextLabel>("%HomeRosterLabel").Text = BuildRosterText(home);
        GetNode<RichTextLabel>("%AwayRosterLabel").Text = BuildRosterText(away);
        GetNode<Button>("%ModeButton").Text = _controlMode == MatchControlMode.PlayerVsCpu
            ? "Opponent: CPU"
            : "Opponent: Player 2";
        GetNode<Label>("%ControlsLabel").Text = _controlMode == MatchControlMode.PlayerVsCpu
            ? "Player 1: Arrows + J/K or Controller"
            : "Player 1: Arrows + J/K or Controller    Player 2: WASD + F/G";

        ArcadeUiStyler.ApplyValueLabel(homeTeamLabel, home.Team.PrimaryColor.Lightened(0.35f), 28);
        ArcadeUiStyler.ApplyValueLabel(awayTeamLabel, away.Team.PrimaryColor.Lightened(0.30f), 28);
        ArcadeUiStyler.ApplyHeroPanel(
            GetNode<PanelContainer>("Margin/VBox/Columns/HomePanel"),
            home.Team.PrimaryColor.Darkened(0.72f),
            home.Team.AccentColor.Lightened(0.20f));
        ArcadeUiStyler.ApplyHeroPanel(
            GetNode<PanelContainer>("Margin/VBox/Columns/AwayPanel"),
            away.Team.PrimaryColor.Darkened(0.70f),
            away.Team.AccentColor.Lightened(0.18f));
    }

    private static string BuildRosterText(NationalTeamProfile profile)
    {
        var roles = profile.Team.Roles.Select(role => PocketPitchConfig.RoleName(role)).ToArray();
        return string.Join('\n', profile.PlayerNames.Select((name, index) => $"{name} - {roles[index]}"));
    }
}
