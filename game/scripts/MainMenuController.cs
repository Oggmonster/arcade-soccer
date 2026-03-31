using System;
using Godot;

public partial class MainMenuController : Control
{
    public event Action? StartRequested;
    public event Action? QuitRequested;

    public override void _Ready()
    {
        GetNode<Button>("%StartButton").Pressed += () => StartRequested?.Invoke();
        GetNode<Button>("%QuitButton").Pressed += () => QuitRequested?.Invoke();
    }
}
