using System;
using Godot;

public partial class GameManager : Node
{
    private const float MoveDeadzone = 0.25f;
    private Node? _currentScreen;
    private MatchSettings _lastQuickMatchSettings = new(
        NationalTeamDatabase.GetByName("Brazil").Team,
        NationalTeamDatabase.GetByName("Italy").Team,
        true,
        false,
        "Rematch",
        "Return to Menu",
        "Quick Match");
    private WorldCupTournament? _activeWorldCup;

    public override void _Ready()
    {
        EnsureInputBindings();
        ShowMainMenu();
    }

    private void EnsureInputBindings()
    {
        EnsureUiBindings();

        ResetAction("move_up");
        AddKeys("move_up", Key.Up);
        AddJoyButtons("move_up", 11);
        AddJoyAxis("move_up", 1, -1f);

        ResetAction("move_down");
        AddKeys("move_down", Key.Down);
        AddJoyButtons("move_down", 12);
        AddJoyAxis("move_down", 1, 1f);

        ResetAction("move_left");
        AddKeys("move_left", Key.Left);
        AddJoyButtons("move_left", 13);
        AddJoyAxis("move_left", 0, -1f);

        ResetAction("move_right");
        AddKeys("move_right", Key.Right);
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

        ResetAction("p2_move_up");
        AddKeys("p2_move_up", Key.W);

        ResetAction("p2_move_down");
        AddKeys("p2_move_down", Key.S);

        ResetAction("p2_move_left");
        AddKeys("p2_move_left", Key.A);

        ResetAction("p2_move_right");
        AddKeys("p2_move_right", Key.D);

        ResetAction("p2_action_a");
        AddKeys("p2_action_a", Key.F);

        ResetAction("p2_action_b");
        AddKeys("p2_action_b", Key.G);
    }

    private static void EnsureUiBindings()
    {
        EnsureAction("ui_up");
        EnsureAction("ui_down");
        EnsureAction("ui_left");
        EnsureAction("ui_right");
        EnsureAction("ui_accept");
        EnsureAction("ui_cancel");

        EnsureJoyButton("ui_up", 11);
        EnsureJoyAxis("ui_up", 1, -1f);
        EnsureJoyButton("ui_down", 12);
        EnsureJoyAxis("ui_down", 1, 1f);
        EnsureJoyButton("ui_left", 13);
        EnsureJoyAxis("ui_left", 0, -1f);
        EnsureJoyButton("ui_right", 14);
        EnsureJoyAxis("ui_right", 0, 1f);
        EnsureJoyButton("ui_accept", 0);
        EnsureJoyButton("ui_cancel", 1);
    }

    private static void EnsureAction(string actionName)
    {
        if (!InputMap.HasAction(actionName))
        {
            InputMap.AddAction(actionName);
        }

        InputMap.ActionSetDeadzone(actionName, MoveDeadzone);
    }

    private static void ResetAction(string actionName)
    {
        EnsureAction(actionName);

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

    private static void EnsureJoyButton(string actionName, int button)
    {
        var inputEvent = new InputEventJoypadButton
        {
            ButtonIndex = (JoyButton)button
        };

        if (!InputMap.ActionHasEvent(actionName, inputEvent))
        {
            InputMap.ActionAddEvent(actionName, inputEvent);
        }
    }

    private static void EnsureJoyAxis(string actionName, int axis, float direction)
    {
        var inputEvent = new InputEventJoypadMotion
        {
            Axis = (JoyAxis)axis,
            AxisValue = direction
        };

        if (!InputMap.ActionHasEvent(actionName, inputEvent))
        {
            InputMap.ActionAddEvent(actionName, inputEvent);
        }
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
        menu.QuickMatchRequested += ShowQuickMatchSetup;
        menu.WorldCupRequested += ShowWorldCupSetup;
        menu.QuitRequested += HandleQuitRequested;
        SetScreen(menu);
    }

    private void ShowQuickMatchSetup()
    {
        var scene = GD.Load<PackedScene>("res://scenes/QuickMatch.tscn");
        var quickMatch = scene.Instantiate<QuickMatchController>();
        quickMatch.BackRequested += ShowMainMenu;
        quickMatch.MatchRequested += StartQuickMatch;
        SetScreen(quickMatch);
    }

    private void ShowWorldCupSetup()
    {
        var scene = GD.Load<PackedScene>("res://scenes/WorldCupSetup.tscn");
        var setup = scene.Instantiate<WorldCupSetupController>();
        setup.BackRequested += ShowMainMenu;
        setup.StartRequested += StartWorldCup;
        SetScreen(setup);
    }

    private void StartQuickMatch(MatchSettings settings)
    {
        _lastQuickMatchSettings = settings;
        StartMatch(settings, OnQuickMatchPrimaryAction);
    }

    private void StartWorldCup(string playerTeamName)
    {
        _activeWorldCup = new WorldCupTournament(playerTeamName);
        ShowWorldCupHub();
    }

    private void ShowWorldCupHub()
    {
        if (_activeWorldCup == null)
        {
            ShowMainMenu();
            return;
        }

        var scene = GD.Load<PackedScene>("res://scenes/WorldCupHub.tscn");
        var hub = scene.Instantiate<WorldCupHubController>();
        hub.BackRequested += ShowMainMenu;
        hub.PlayMatchRequested += StartWorldCupMatch;
        SetScreen(hub);
        hub.Configure(_activeWorldCup);
    }

    private void StartWorldCupMatch()
    {
        if (_activeWorldCup == null || !_activeWorldCup.HasPlayableMatch)
        {
            ShowWorldCupHub();
            return;
        }

        StartMatch(_activeWorldCup.CreateNextMatchSettings(), OnWorldCupPrimaryAction);
    }

    private void StartMatch(MatchSettings settings, Action<MatchResult>? primaryAction)
    {
        var scene = GD.Load<PackedScene>("res://scenes/Match.tscn");
        var match = scene.Instantiate<MatchManager>();
        match.Configure(settings);
        match.ReturnToMenuRequested += ShowMainMenu;
        if (primaryAction != null)
        {
            match.MatchCompleted += primaryAction;
        }
        SetScreen(match);
    }

    private void OnQuickMatchPrimaryAction(MatchResult _)
    {
        StartQuickMatch(_lastQuickMatchSettings);
    }

    private void OnWorldCupPrimaryAction(MatchResult result)
    {
        if (_activeWorldCup == null)
        {
            ShowMainMenu();
            return;
        }

        _activeWorldCup.ApplyPlayedMatch(result);
        ShowWorldCupHub();
    }

    private void HandleQuitRequested()
    {
        GetTree().Quit();
    }
}
