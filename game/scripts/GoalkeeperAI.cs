using Godot;

public partial class GoalkeeperAI : Node2D, IPitchAgent
{
    private Vector2 _velocity;
    private float _holdTimer;
    private Rect2 _roamRect;

    public TeamSide TeamSide { get; private set; }
    public Color PrimaryColor { get; private set; } = Colors.CornflowerBlue;
    public Color AccentColor { get; private set; } = Colors.White;
    public Vector2 GoalCenter { get; private set; }
    public Vector2 FacingDirection { get; private set; } = Vector2.Up;
    public float Radius => 24f;
    public float PickupRadius => 36f;
    public float BallCarryDistance => Radius - 2f;
    public bool CanReceiveBall => true;
    public bool ReadyToDistribute => _holdTimer <= 0.01f;

    public void Configure(TeamSide side, Color primaryColor, Color accentColor, Vector2 goalCenter, Rect2 roamRect)
    {
        TeamSide = side;
        PrimaryColor = primaryColor;
        AccentColor = accentColor;
        GoalCenter = goalCenter;
        _roamRect = roamRect;
        ResetForKickoff();
    }

    public void ResetForKickoff()
    {
        GlobalPosition = new Vector2(GoalCenter.X, TeamSide == TeamSide.Home ? GoalCenter.Y - 34f : GoalCenter.Y + 34f);
        FacingDirection = TeamSide == TeamSide.Home ? Vector2.Up : Vector2.Down;
        _velocity = Vector2.Zero;
        _holdTimer = 0f;
        QueueRedraw();
    }

    public void Simulate(double delta, Vector2 ballPosition, bool hasBall)
    {
        if (_holdTimer > 0f)
        {
            _holdTimer = Mathf.Max(0f, _holdTimer - (float)delta);
        }

        Vector2 targetPosition;
        if (hasBall)
        {
            targetPosition = new Vector2(
                GoalCenter.X,
                TeamSide == TeamSide.Home ? GoalCenter.Y - 40f : GoalCenter.Y + 40f);
            FacingDirection = TeamSide == TeamSide.Home ? Vector2.Up : Vector2.Down;
        }
        else
        {
            targetPosition = new Vector2(
                Mathf.Clamp(ballPosition.X, _roamRect.Position.X + 30f, _roamRect.End.X - 30f),
                Mathf.Clamp(GoalCenter.Y + ((ballPosition.Y - GoalCenter.Y) * 0.18f), _roamRect.Position.Y + 20f, _roamRect.End.Y - 20f));
            var toBall = ballPosition - GlobalPosition;
            if (toBall.LengthSquared() > 0.01f)
            {
                FacingDirection = toBall.Normalized();
            }
        }

        var desired = targetPosition - GlobalPosition;
        var desiredDirection = desired.LengthSquared() > 0.1f ? desired.Normalized() : Vector2.Zero;
        _velocity = _velocity.MoveToward(desiredDirection * 215f, 900f * (float)delta);
        GlobalPosition += _velocity * (float)delta;
        ClampToRoamRect();
        QueueRedraw();
    }

    public void CaptureBall(float holdSeconds = 0.45f)
    {
        _holdTimer = holdSeconds;
        QueueRedraw();
    }

    public void ApplySeparation(Vector2 offset)
    {
        GlobalPosition += offset;
        ClampToRoamRect();
    }

    public override void _Draw()
    {
        DrawEllipse(new Vector2(0f, 9f), Radius, 12f, new Color(0f, 0f, 0f, 0.18f));
        DrawRect(new Rect2(-Radius, -Radius + 2f, Radius * 2f, Radius * 2f), PrimaryColor, true);
        DrawRect(new Rect2(-Radius + 4f, -Radius + 6f, Radius * 2f - 8f, Radius * 2f - 12f), AccentColor, false, 3f);
        DrawLine(Vector2.Zero, FacingDirection * (Radius + 4f), AccentColor, 3f);
        DrawString(
            ThemeDB.FallbackFont,
            new Vector2(-7f, 7f),
            "GK",
            HorizontalAlignment.Left,
            -1f,
            16,
            new Color("112230"));
    }

    private void ClampToRoamRect()
    {
        GlobalPosition = new Vector2(
            Mathf.Clamp(GlobalPosition.X, _roamRect.Position.X + Radius, _roamRect.End.X - Radius),
            Mathf.Clamp(GlobalPosition.Y, _roamRect.Position.Y + Radius, _roamRect.End.Y - Radius));
    }
}
