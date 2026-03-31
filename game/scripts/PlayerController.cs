using Godot;

public partial class PlayerController : Node2D, IPitchAgent
{
    private Vector2 _velocity;
    private Vector2 _desiredDirection = Vector2.Zero;
    private float _recoveryTimer;
    private float _slideTimer;
    private float _flashTimer;
    private bool _slideHitConsumed;

    public TeamSide TeamSide { get; private set; }
    public int SlotIndex { get; private set; }
    public PlayerRole Role { get; private set; }
    public StatBlock Stats { get; private set; } = PocketPitchConfig.CreateStats(PlayerRole.Medium);
    public Vector2 SpawnAnchor { get; private set; }
    public Vector2 FacingDirection { get; private set; } = Vector2.Up;
    public Color PrimaryColor { get; private set; } = Colors.CornflowerBlue;
    public Color AccentColor { get; private set; } = Colors.White;
    public bool IsHumanControlled { get; private set; }
    public bool HasBallMarker { get; private set; }

    public float Radius => Stats.BodyRadius;
    public float PickupRadius => Radius + 20f;
    public float BallCarryDistance => Radius + Stats.DribbleDistance;
    public bool CanReceiveBall => _recoveryTimer <= 0.28f;
    public bool CanAct => _recoveryTimer <= 0.02f && _slideTimer <= 0.02f;
    public bool IsSliding => _slideTimer > 0f;
    public bool SlideHitAvailable => IsSliding && !_slideHitConsumed;

    public void Configure(
        TeamSide side,
        int slotIndex,
        PlayerRole role,
        Color primaryColor,
        Color accentColor,
        Vector2 spawnAnchor)
    {
        TeamSide = side;
        SlotIndex = slotIndex;
        Role = role;
        Stats = PocketPitchConfig.CreateStats(role);
        PrimaryColor = primaryColor;
        AccentColor = accentColor;
        SpawnAnchor = spawnAnchor;
        ResetForPosition(spawnAnchor, side == TeamSide.Home ? Vector2.Up : Vector2.Down);
    }

    public void ResetForPosition(Vector2 position, Vector2 facing)
    {
        GlobalPosition = position;
        FacingDirection = facing;
        _velocity = Vector2.Zero;
        _desiredDirection = Vector2.Zero;
        _recoveryTimer = 0f;
        _slideTimer = 0f;
        _flashTimer = 0f;
        _slideHitConsumed = false;
        QueueRedraw();
    }

    public void SetDesiredDirection(Vector2 direction)
    {
        _desiredDirection = direction.LengthSquared() > 0.0001f ? direction.Normalized() : Vector2.Zero;
    }

    public void SetControlState(bool isHumanControlled, bool hasBallMarker)
    {
        IsHumanControlled = isHumanControlled;
        HasBallMarker = hasBallMarker;
        QueueRedraw();
    }

    public void Simulate(double delta, bool hasBall)
    {
        if (_flashTimer > 0f)
        {
            _flashTimer = Mathf.Max(0f, _flashTimer - (float)delta);
        }

        if (_recoveryTimer > 0f)
        {
            _recoveryTimer = Mathf.Max(0f, _recoveryTimer - (float)delta);
        }

        if (_slideTimer > 0f)
        {
            _slideTimer = Mathf.Max(0f, _slideTimer - (float)delta);
            GlobalPosition += _velocity * (float)delta;
            _velocity = _velocity.MoveToward(Vector2.Zero, Stats.AccelerationUnits * 1.2f * (float)delta);
            if (_slideTimer <= 0f)
            {
                _velocity *= 0.3f;
            }
        }
        else
        {
            if (_desiredDirection != Vector2.Zero)
            {
                FacingDirection = _desiredDirection;
            }

            var speedPenalty = hasBall ? 0.9f : 1f;
            var recoveryPenalty = _recoveryTimer > 0f ? 0.45f : 1f;
            var targetVelocity = _desiredDirection * Stats.MoveSpeed * speedPenalty * recoveryPenalty;
            _velocity = _velocity.MoveToward(targetVelocity, Stats.AccelerationUnits * (float)delta);
            GlobalPosition += _velocity * (float)delta;
        }

        QueueRedraw();
    }

    public bool BeginSlide(Vector2 direction)
    {
        if (!CanAct)
        {
            return false;
        }

        var slideDirection = direction.LengthSquared() > 0.0001f ? direction.Normalized() : FacingDirection;
        FacingDirection = slideDirection;
        _velocity = slideDirection * Stats.SlideSpeed;
        _slideTimer = 0.17f;
        _recoveryTimer = Mathf.Max(_recoveryTimer, Stats.RecoverySeconds + 0.18f);
        _slideHitConsumed = false;
        _flashTimer = 0.15f;
        QueueRedraw();
        return true;
    }

    public void ConsumeSlideHit()
    {
        _slideHitConsumed = true;
    }

    public void ApplyRecovery(float durationSeconds)
    {
        _recoveryTimer = Mathf.Max(_recoveryTimer, durationSeconds);
    }

    public void FlashAction()
    {
        _flashTimer = 0.16f;
        QueueRedraw();
    }

    public void ClampToField(Rect2 rect)
    {
        GlobalPosition = new Vector2(
            Mathf.Clamp(GlobalPosition.X, rect.Position.X + Radius, rect.End.X - Radius),
            Mathf.Clamp(GlobalPosition.Y, rect.Position.Y + Radius, rect.End.Y - Radius));
    }

    public void ApplySeparation(Vector2 offset)
    {
        GlobalPosition += offset;
    }

    public override void _Draw()
    {
        var shadowColor = new Color(0f, 0f, 0f, 0.18f);
        var bodyColor = _flashTimer > 0f ? AccentColor : PrimaryColor;
        var outlineColor = HasBallMarker ? new Color("fff275") : AccentColor;
        var indicatorColor = IsHumanControlled ? new Color("7ef8ff") : Colors.Transparent;

        DrawEllipse(new Vector2(0f, Radius * 0.55f), Radius, Radius * 0.62f, shadowColor);
        DrawCircle(Vector2.Zero, Radius, bodyColor);
        DrawArc(Vector2.Zero, Radius + 1.5f, 0f, Mathf.Tau, 28, outlineColor, 3f);
        DrawLine(Vector2.Zero, FacingDirection * Radius * 0.95f, outlineColor, 3f);

        if (IsHumanControlled)
        {
            DrawArc(Vector2.Zero, Radius + 10f, 0f, Mathf.Tau, 32, indicatorColor, 3f);
            DrawCircle(new Vector2(0f, -(Radius + 16f)), 4f, indicatorColor);
        }

        var initial = Role switch
        {
            PlayerRole.Big => "B",
            PlayerRole.Medium => "M",
            _ => "S"
        };

        DrawString(
            ThemeDB.FallbackFont,
            new Vector2(-7f, 7f),
            initial,
            HorizontalAlignment.Left,
            -1f,
            16,
            new Color("102030"));
    }
}
