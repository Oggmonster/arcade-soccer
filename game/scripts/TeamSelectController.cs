using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class TeamSelectController : Control
{
    public event Action? BackRequested;
    public event Action<TeamDefinition, TeamDefinition>? MatchRequested;

    private readonly List<Button> _homeRoleButtons = new();
    private IReadOnlyList<TeamDefinition> _awayPresets = PocketPitchConfig.CreateAwayPresets();
    private TeamDefinition _homeTeam = PocketPitchConfig.DefaultHomeTeam.Clone();
    private TeamDefinition _awayTeam = PocketPitchConfig.AwayPresets[0].Clone();
    private int _awayPresetIndex;

    public override void _Ready()
    {
        for (var i = 0; i < 5; i++)
        {
            var button = GetNode<Button>($"%HomeSlot{i}");
            var index = i;
            button.Pressed += () => CycleHomeRole(index);
            _homeRoleButtons.Add(button);
        }

        GetNode<Button>("%AwayPresetButton").Pressed += CycleAwayPreset;
        GetNode<Button>("%BackButton").Pressed += () => BackRequested?.Invoke();
        GetNode<Button>("%StartMatchButton").Pressed += () => MatchRequested?.Invoke(_homeTeam.Clone(), _awayTeam.Clone());
        RefreshUi();
    }

    public void Configure(TeamDefinition homeTeam, TeamDefinition awayTeam)
    {
        _homeTeam = homeTeam.Clone();
        _awayPresets = PocketPitchConfig.CreateAwayPresets();
        _awayPresetIndex = _awayPresets
            .Select((preset, index) => (preset, index))
            .FirstOrDefault(entry => entry.preset.Name == awayTeam.Name)
            .index;
        _awayTeam = _awayPresets[Mathf.Clamp(_awayPresetIndex, 0, _awayPresets.Count - 1)].Clone();
        if (IsNodeReady())
        {
            RefreshUi();
        }
    }

    private void CycleHomeRole(int index)
    {
        _homeTeam.Roles[index] = PocketPitchConfig.NextRole(_homeTeam.Roles[index]);
        RefreshUi();
    }

    private void CycleAwayPreset()
    {
        _awayPresetIndex = (_awayPresetIndex + 1) % _awayPresets.Count;
        _awayTeam = _awayPresets[_awayPresetIndex].Clone();
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (!IsNodeReady())
        {
            return;
        }

        GetNode<Label>("%HomeTeamLabel").Text = $"{_homeTeam.Name}  ({BuildCompositionLabel(_homeTeam)})";
        GetNode<Label>("%AwayTeamLabel").Text = $"{_awayTeam.Name}  ({BuildCompositionLabel(_awayTeam)})";

        for (var i = 0; i < _homeRoleButtons.Count; i++)
        {
            _homeRoleButtons[i].Text = $"{PocketPitchConfig.SquadSlots[i]}: {PocketPitchConfig.RoleName(_homeTeam.Roles[i])}";
            _homeRoleButtons[i].Modulate = RoleColor(_homeTeam.Roles[i]);
        }

        GetNode<Button>("%AwayPresetButton").Text = $"Rotate CPU Team: {_awayTeam.Name}";
        GetNode<RichTextLabel>("%AwayRolesLabel").Text =
            string.Join('\n', _awayTeam.Roles.Select((role, index) =>
                $"{PocketPitchConfig.SquadSlots[index]}: {PocketPitchConfig.RoleName(role)}"));
    }

    private static string BuildCompositionLabel(TeamDefinition team)
    {
        var big = team.Roles.Count(role => role == PlayerRole.Big);
        var medium = team.Roles.Count(role => role == PlayerRole.Medium);
        var small = team.Roles.Count(role => role == PlayerRole.Small);
        return $"{big} Big / {medium} Medium / {small} Small";
    }

    private static Color RoleColor(PlayerRole role)
    {
        return role switch
        {
            PlayerRole.Big => new Color("f2c14e"),
            PlayerRole.Medium => new Color("d7ebff"),
            _ => new Color("9ef0b0")
        };
    }
}
