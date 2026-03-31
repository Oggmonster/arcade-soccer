using Godot;

public partial class GameManager : Node
{
    private const float MoveDeadzone = 0.25f;
    private Node? _currentScreen;
    private TeamDefinition _lastHomeTeam = PocketPitchConfig.DefaultHomeTeam.Clone();
    private TeamDefinition _lastAwayTeam = PocketPitchConfig.AwayPresets[0].Clone();

    public override void _Ready()
    {
        EnsureInputBindings();
        ShowMainMenu();
    }

    private void EnsureInputBindings()
    {
        ResetAction("move_up");
        AddKeys("move_up", Key.Up, Key.W);
        AddJoyButtons("move_up", 11);
        AddJoyAxis("move_up", 1, -1f);

        ResetAction("move_down");
        AddKeys("move_down", Key.Down, Key.S);
        AddJoyButtons("move_down", 12);
        AddJoyAxis("move_down", 1, 1f);

        ResetAction("move_left");
        AddKeys("move_left", Key.Left, Key.A);
        AddJoyButtons("move_left", 13);
        AddJoyAxis("move_left", 0, -1f);

        ResetAction("move_right");
        AddKeys("move_right", Key.Right, Key.D);
        AddJoyButtons("move_right", 14);
        AddJoyAxis("move_right", 0, 1f);

        ResetAction("action_a");
        AddKeys("action_a", Key.J);
        AddJoyButtons("action_a", 0);

        ResetAction("action_b");
        AddKeys("action_b", Key.K);
        AddJoyButtons("action_b", 1);

        ResetAction("pause");
        AddKeys("pause", Key.Enter, Key.Escape);
        AddJoyButtons("pause", 6);
    }

    private static void ResetAction(string actionName)
    {
        if (!InputMap.HasAction(actionName))
        {
            InputMap.AddAction(actionName);
        }

        InputMap.ActionSetDeadzone(actionName, MoveDeadzone);

        foreach (var existing in InputMap.ActionGetEvents(actionName))
        {
            InputMap.ActionEraseEvent(actionName, existing);
        }
    }

    private static void AddKeys(string actionName, params Key[] keys)
    {
        foreach (var key in keys)
        {
            var inputEvent = new InputEventKey
            {
                Keycode = key,
                PhysicalKeycode = key
            };
            InputMap.ActionAddEvent(actionName, inputEvent);
        }
    }

    private static void AddJoyButtons(string actionName, params int[] buttons)
    {
        foreach (var button in buttons)
        {
            var inputEvent = new InputEventJoypadButton
            {
                ButtonIndex = (JoyButton)button
            };
            InputMap.ActionAddEvent(actionName, inputEvent);
        }
    }

    private static void AddJoyAxis(string actionName, int axis, float direction)
    {
        var inputEvent = new InputEventJoypadMotion
        {
            Axis = (JoyAxis)axis,
            AxisValue = direction
        };
        InputMap.ActionAddEvent(actionName, inputEvent);
    }

    private void ClearCurrentScreen()
    {
        if (_currentScreen == null)
        {
            return;
        }

        RemoveChild(_currentScreen);
        _currentScreen.QueueFree();
        _currentScreen = null;
    }

    private void SetScreen(Node screen)
    {
        ClearCurrentScreen();
        _currentScreen = screen;
        AddChild(screen);
    }

    private void ShowMainMenu()
    {
        var scene = GD.Load<PackedScene>("res://scenes/MainMenu.tscn");
        var menu = scene.Instantiate<MainMenuController>();
        menu.StartRequested += ShowTeamSelect;
        menu.QuitRequested += HandleQuitRequested;
        SetScreen(menu);
    }

    private void ShowTeamSelect()
    {
        var scene = GD.Load<PackedScene>("res://scenes/TeamSelect.tscn");
        var teamSelect = scene.Instantiate<TeamSelectController>();
        teamSelect.Configure(_lastHomeTeam.Clone(), _lastAwayTeam.Clone());
        teamSelect.BackRequested += ShowMainMenu;
        teamSelect.MatchRequested += StartMatch;
        SetScreen(teamSelect);
    }

    private void StartMatch(TeamDefinition homeTeam, TeamDefinition awayTeam)
    {
        _lastHomeTeam = homeTeam.Clone();
        _lastAwayTeam = awayTeam.Clone();

        var scene = GD.Load<PackedScene>("res://scenes/Match.tscn");
        var match = scene.Instantiate<MatchManager>();
        match.Configure(_lastHomeTeam.Clone(), _lastAwayTeam.Clone());
        match.ReturnToMenuRequested += ShowMainMenu;
        match.RematchRequested += () => StartMatch(_lastHomeTeam.Clone(), _lastAwayTeam.Clone());
        SetScreen(match);
    }

    private void HandleQuitRequested()
    {
        GetTree().Quit();
    }
}
