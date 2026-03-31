using System;
using Godot;

public partial class HUDController : CanvasLayer
{
    public event Action? RematchRequested;
    public event Action? MenuRequested;

    private Label? _scoreLabel;
    private Label? _timerLabel;
    private Label? _statusLabel;
    private ProgressBar? _chargeBar;
    private PanelContainer? _fullTimePanel;
    private Label? _fullTimeLabel;

    public override void _Ready()
    {
        _scoreLabel = GetNode<Label>("%ScoreLabel");
        _timerLabel = GetNode<Label>("%TimerLabel");
        _statusLabel = GetNode<Label>("%StatusLabel");
        _chargeBar = GetNode<ProgressBar>("%ShotChargeBar");
        _fullTimePanel = GetNode<PanelContainer>("%FullTimePanel");
        _fullTimeLabel = GetNode<Label>("%FullTimeLabel");

        GetNode<Button>("%RematchButton").Pressed += () => RematchRequested?.Invoke();
        GetNode<Button>("%MenuButton").Pressed += () => MenuRequested?.Invoke();
        HideFullTime();
    }

    public void UpdateScore(int homeScore, int awayScore, string homeName, string awayName)
    {
        _scoreLabel!.Text = $"{homeName}  {homeScore} - {awayScore}  {awayName}";
    }

    public void UpdateTimer(float remainingSeconds)
    {
        var clamped = Mathf.Max(0f, remainingSeconds);
        var minutes = Mathf.FloorToInt(clamped / 60f);
        var seconds = Mathf.FloorToInt(clamped % 60f);
        _timerLabel!.Text = $"{minutes}:{seconds:00}";
    }

    public void SetStatus(string text)
    {
        _statusLabel!.Text = text;
        _statusLabel.Visible = !string.IsNullOrWhiteSpace(text);
    }

    public void SetShotCharge(float charge)
    {
        _chargeBar!.Value = Mathf.Clamp(charge, 0f, 1f) * 100f;
        _chargeBar.Visible = charge > 0.01f;
    }

    public void ShowFullTime(string summary)
    {
        _fullTimePanel!.Visible = true;
        _fullTimeLabel!.Text = summary;
    }

    public void HideFullTime()
    {
        _fullTimePanel!.Visible = false;
        _chargeBar!.Visible = false;
        SetStatus(string.Empty);
    }
}
