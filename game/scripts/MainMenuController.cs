using System;
using Godot;

public partial class MainMenuController : Control
{
    public event Action? QuickMatchRequested;
    public event Action? WorldCupRequested;
    public event Action? QuitRequested;

    public override void _Ready()
    {
        GetNode<Button>("%QuickMatchButton").Pressed += () => QuickMatchRequested?.Invoke();
        GetNode<Button>("%WorldCupButton").Pressed += () => WorldCupRequested?.Invoke();
        GetNode<Button>("%QuitButton").Pressed += () => QuitRequested?.Invoke();
    }
}
