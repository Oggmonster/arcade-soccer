using Godot;

public partial class BallController : Node2D
{
    private IBallCarrier? _owner;
    private Vector2 _velocity;
    private Node? _claimLockedNode;
    private Node? _secondaryClaimLockedNode;
    private float _claimLockTimer;
    private float _secondaryClaimLockTimer;

    public IBallCarrier? Carrier => _owner;
    public Vector2 Velocity => _velocity;
    public TeamSide? CarrierSide => _owner?.TeamSide;
    public float Speed => _velocity.Length();

    public override void _Draw()
    {
        DrawCircle(new Vector2(0f, 4f), PocketPitchConfig.BallRadius, new Color(0f, 0f, 0f, 0.22f));
        DrawCircle(Vector2.Zero, PocketPitchConfig.BallRadius, Colors.WhiteSmoke);
        DrawArc(Vector2.Zero, PocketPitchConfig.BallRadius - 2f, 0f, Mathf.Tau, 18, new Color("24313d"), 1.5f);
    }

    public void AttachTo(IBallCarrier owner)
    {
        _owner = owner;
        _velocity = Vector2.Zero;
        _claimLockTimer = 0f;
        _secondaryClaimLockTimer = 0f;
        _claimLockedNode = null;
        _secondaryClaimLockedNode = null;
        SnapToOwner();
        QueueRedraw();
    }

    public void KickFrom(IBallCarrier owner, Vector2 velocity, float claimLockSeconds = 0.14f)
    {
        _owner = null;
        _velocity = velocity;
        _claimLockTimer = claimLockSeconds;
        _claimLockedNode = owner as Node;
        GlobalPosition = owner.GlobalPosition + (owner.FacingDirection * owner.BallCarryDistance);
        QueueRedraw();
    }

    public bool CanBeClaimedBy(Node candidate)
    {
        var blockedByPrimary = _claimLockTimer > 0f && candidate == _claimLockedNode;
        var blockedBySecondary = _secondaryClaimLockTimer > 0f && candidate == _secondaryClaimLockedNode;
        return !blockedByPrimary && !blockedBySecondary;
    }

    public void LockClaimsAgainst(Node candidate, float claimLockSeconds)
    {
        _secondaryClaimLockedNode = candidate;
        _secondaryClaimLockTimer = Mathf.Max(_secondaryClaimLockTimer, claimLockSeconds);
    }

    public void Simulate(double delta)
    {
        if (_claimLockTimer > 0f)
        {
            _claimLockTimer = Mathf.Max(0f, _claimLockTimer - (float)delta);
        }

        if (_secondaryClaimLockTimer > 0f)
        {
            _secondaryClaimLockTimer = Mathf.Max(0f, _secondaryClaimLockTimer - (float)delta);
        }

        if (_owner != null)
        {
            SnapToOwner();
            return;
        }

        GlobalPosition += _velocity * (float)delta;
        _velocity = _velocity.MoveToward(Vector2.Zero, PocketPitchConfig.BallFriction * (float)delta);
    }

    public void BounceX(float x)
    {
        GlobalPosition = new Vector2(x, GlobalPosition.Y);
        _velocity = new Vector2(-_velocity.X * PocketPitchConfig.WallBounce, _velocity.Y);
    }

    public void BounceY(float y)
    {
        GlobalPosition = new Vector2(GlobalPosition.X, y);
        _velocity = new Vector2(_velocity.X, -_velocity.Y * PocketPitchConfig.WallBounce);
    }

    public void Stop()
    {
        _velocity = Vector2.Zero;
    }

    private void SnapToOwner()
    {
        if (_owner == null)
        {
            return;
        }

        GlobalPosition = _owner.GlobalPosition + (_owner.FacingDirection * _owner.BallCarryDistance);
    }
}
