using System;
using System.Collections.Generic;
using Godot;

public partial class MatchManager : Node2D
{
    private const float GoalkeeperClaimSpeedThreshold = 760f;
    private const float GoalkeeperPressureDistance = 84f;
    public event Action? ReturnToMenuRequested;
    public event Action? RematchRequested;

    private readonly RandomNumberGenerator _rng = new();
    private readonly List<PlayerController> _homePlayers = new();
    private readonly List<PlayerController> _awayPlayers = new();

    private PackedScene? _playerScene;
    private PackedScene? _goalkeeperScene;
    private BallController? _ball;
    private HUDController? _hud;
    private Camera2D? _camera;
    private Node2D? _playersRoot;
    private Node2D? _goalkeepersRoot;
    private GoalkeeperAI? _homeKeeper;
    private GoalkeeperAI? _awayKeeper;
    private PlayerController? _controlledPlayer;
    private TeamDefinition _homeTeam = PocketPitchConfig.DefaultHomeTeam.Clone();
    private TeamDefinition _awayTeam = PocketPitchConfig.AwayPresets[0].Clone();
    private MatchPhase _phase = MatchPhase.PreKickoff;
    private MatchPhase _phaseBeforePause = MatchPhase.InPlay;
    private TeamSide _nextKickoffTeam = TeamSide.Home;
    private float _phaseTimer;
    private float _matchTimeRemaining = PocketPitchConfig.MatchLengthSeconds;
    private float _statusTimer;
    private int _homeScore;
    private int _awayScore;

    public void Configure(TeamDefinition homeTeam, TeamDefinition awayTeam)
    {
        _homeTeam = homeTeam.Clone();
        _awayTeam = awayTeam.Clone();
    }

    public override void _Ready()
    {
        _rng.Randomize();
        _playerScene = GD.Load<PackedScene>("res://scenes/Player.tscn");
        _goalkeeperScene = GD.Load<PackedScene>("res://scenes/Goalkeeper.tscn");
        _ball = GetNode<BallController>("Ball");
        _hud = GetNode<HUDController>("HUD");
        _camera = GetNode<Camera2D>("Camera2D");
        _playersRoot = GetNode<Node2D>("Players");
        _goalkeepersRoot = GetNode<Node2D>("Goalkeepers");

        _hud.RematchRequested += () => RematchRequested?.Invoke();
        _hud.MenuRequested += () => ReturnToMenuRequested?.Invoke();
        BuildTeams();
        StartNewMatch();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_camera != null)
        {
            _camera.GlobalPosition = _camera.GlobalPosition.Lerp(_ball!.GlobalPosition, 0.11f);
        }

        if (Input.IsActionJustPressed("pause") && _phase != MatchPhase.FullTime)
        {
            TogglePause();
        }

        UpdateStatus(delta);

        if (_phase == MatchPhase.Paused || _phase == MatchPhase.FullTime)
        {
            UpdateHud();
            return;
        }

        if (_phase == MatchPhase.GoalScored)
        {
            _phaseTimer -= (float)delta;
            if (_phaseTimer <= 0f)
            {
                SetupKickoff(_nextKickoffTeam);
            }

            UpdateHud();
            return;
        }

        if (_phase == MatchPhase.PreKickoff)
        {
            _phaseTimer -= (float)delta;
            if (_phaseTimer <= 0f)
            {
                _phase = MatchPhase.InPlay;
                SetStatus("Play!", 0.5f);
            }

            SimulateActors(delta);
            UpdateHud();
            return;
        }

        _matchTimeRemaining = Mathf.Max(0f, _matchTimeRemaining - (float)delta);
        if (_matchTimeRemaining <= 0f)
        {
            FinishMatch();
            UpdateHud();
            return;
        }

        UpdateControlledPlayer();
        HandleInput(delta);
        RunAi();
        SimulateActors(delta);
        ResolveSeparation();
        ClampPlayers();
        EnforceGoalkeeperProtection();
        CheckSlideTackles();
        _ball!.Simulate(delta);
        ProcessGoalkeepers();
        if (CheckGoalScored())
        {
            UpdateHud();
            return;
        }

        KeepBallInPlay();
        ClaimLooseBall();
        UpdateHud();
    }

    private void BuildTeams()
    {
        foreach (var existing in _homePlayers)
        {
            existing.QueueFree();
        }

        foreach (var existing in _awayPlayers)
        {
            existing.QueueFree();
        }

        _homePlayers.Clear();
        _awayPlayers.Clear();
        _homeKeeper?.QueueFree();
        _awayKeeper?.QueueFree();

        for (var i = 0; i < 5; i++)
        {
            var homePlayer = _playerScene!.Instantiate<PlayerController>();
            homePlayer.Configure(TeamSide.Home, i, _homeTeam.Roles[i], _homeTeam.PrimaryColor, _homeTeam.AccentColor, PocketPitchConfig.HomeAnchors[i]);
            _playersRoot!.AddChild(homePlayer);
            _homePlayers.Add(homePlayer);

            var awayPlayer = _playerScene.Instantiate<PlayerController>();
            awayPlayer.Configure(TeamSide.Away, i, _awayTeam.Roles[i], _awayTeam.PrimaryColor, _awayTeam.AccentColor, PocketPitchConfig.AwayAnchors[i]);
            _playersRoot.AddChild(awayPlayer);
            _awayPlayers.Add(awayPlayer);
        }

        _homeKeeper = _goalkeeperScene!.Instantiate<GoalkeeperAI>();
        _homeKeeper.Configure(
            TeamSide.Home,
            _homeTeam.PrimaryColor.Darkened(0.12f),
            _homeTeam.AccentColor,
            PocketPitchConfig.GoalCenter(TeamSide.Home),
            PocketPitchConfig.GoalkeeperRoamRect(TeamSide.Home));
        _goalkeepersRoot!.AddChild(_homeKeeper);

        _awayKeeper = _goalkeeperScene.Instantiate<GoalkeeperAI>();
        _awayKeeper.Configure(
            TeamSide.Away,
            _awayTeam.PrimaryColor.Darkened(0.12f),
            _awayTeam.AccentColor,
            PocketPitchConfig.GoalCenter(TeamSide.Away),
            PocketPitchConfig.GoalkeeperRoamRect(TeamSide.Away));
        _goalkeepersRoot.AddChild(_awayKeeper);
    }

    private void StartNewMatch()
    {
        _homeScore = 0;
        _awayScore = 0;
        _matchTimeRemaining = PocketPitchConfig.MatchLengthSeconds;
        _phase = MatchPhase.PreKickoff;
        _phaseBeforePause = MatchPhase.InPlay;
        _nextKickoffTeam = TeamSide.Home;
        _hud!.HideFullTime();
        SetupKickoff(_nextKickoffTeam);
    }

    private void SetupKickoff(TeamSide kickoffTeam)
    {
        _phase = MatchPhase.PreKickoff;
        _phaseTimer = PocketPitchConfig.KickoffDelaySeconds;

        for (var i = 0; i < _homePlayers.Count; i++)
        {
            var homePosition = PocketPitchConfig.HomeAnchors[i];
            if (kickoffTeam == TeamSide.Home && i == 4)
            {
                homePosition = new Vector2(0f, 44f);
            }
            else if (kickoffTeam == TeamSide.Away && i == 4)
            {
                homePosition = new Vector2(0f, 92f);
            }

            _homePlayers[i].ResetForPosition(homePosition, Vector2.Up);
        }

        for (var i = 0; i < _awayPlayers.Count; i++)
        {
            var awayPosition = PocketPitchConfig.AwayAnchors[i];
            if (kickoffTeam == TeamSide.Away && i == 4)
            {
                awayPosition = new Vector2(0f, -44f);
            }
            else if (kickoffTeam == TeamSide.Home && i == 4)
            {
                awayPosition = new Vector2(0f, -92f);
            }

            _awayPlayers[i].ResetForPosition(awayPosition, Vector2.Down);
        }

        _homeKeeper!.ResetForKickoff();
        _awayKeeper!.ResetForKickoff();

        var kickoffPlayer = kickoffTeam == TeamSide.Home ? _homePlayers[4] : _awayPlayers[4];
        _ball!.AttachTo(kickoffPlayer);
        UpdateControlledPlayer();
        SetStatus(kickoffTeam == TeamSide.Home ? "Blue kick off" : "Orange kick off", PocketPitchConfig.KickoffDelaySeconds);
        UpdateHud();
    }

    private void UpdateControlledPlayer()
    {
        if (_ball!.Carrier is PlayerController owner && owner.TeamSide == TeamSide.Home)
        {
            _controlledPlayer = owner;
        }
        else
        {
            var closestDistance = float.MaxValue;
            PlayerController? closest = null;
            foreach (var player in _homePlayers)
            {
                var distance = player.GlobalPosition.DistanceSquaredTo(_ball.GlobalPosition);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = player;
                }
            }

            _controlledPlayer = closest;
        }

        foreach (var player in _homePlayers)
        {
            player.SetControlState(player == _controlledPlayer, _ball.Carrier == player);
        }

        foreach (var player in _awayPlayers)
        {
            player.SetControlState(false, _ball.Carrier == player);
        }
    }

    private void HandleInput(double delta)
    {
        var moveInput = ReadMoveInput();
        foreach (var player in _homePlayers)
        {
            if (player != _controlledPlayer)
            {
                player.SetDesiredDirection(Vector2.Zero);
            }
        }

        foreach (var player in _awayPlayers)
        {
            player.SetDesiredDirection(Vector2.Zero);
        }

        if (_controlledPlayer == null)
        {
            return;
        }

        var humanHasBall = _ball!.Carrier == _controlledPlayer;
        _controlledPlayer.SetDesiredDirection(moveInput);

        if (humanHasBall)
        {
            if (Input.IsActionJustPressed("action_a"))
            {
                AttemptPass(_controlledPlayer, false);
            }

            if (Input.IsActionJustPressed("action_b"))
            {
                AttemptShot(_controlledPlayer, 1.08f);
            }
        }
        else
        {
            if (Input.IsActionJustPressed("action_a"))
            {
                AttemptStandTackle(_controlledPlayer);
            }

            if (Input.IsActionJustPressed("action_b"))
            {
                AttemptSlideTackle(_controlledPlayer, moveInput);
            }
        }
    }

    private void RunAi()
    {
        var homePressing = GetClosestPlayerToBall(_homePlayers);
        var awayPressing = GetClosestPlayerToBall(_awayPlayers);

        foreach (var player in _homePlayers)
        {
            if (player == _controlledPlayer)
            {
                continue;
            }

            UpdateAiPlayer(player, player == homePressing);
        }

        foreach (var player in _awayPlayers)
        {
            UpdateAiPlayer(player, player == awayPressing);
        }
    }

    private void UpdateAiPlayer(PlayerController player, bool isPressingPlayer)
    {
        if (_ball!.Carrier is GoalkeeperAI activeKeeper)
        {
            if (activeKeeper.TeamSide == player.TeamSide)
            {
                var supportTarget = GetSupportTarget(player);
                player.SetDesiredDirection((supportTarget - player.GlobalPosition).Normalized());
            }
            else
            {
                var retreatTarget = GetGoalkeeperRespectTarget(activeKeeper.TeamSide, player);
                player.SetDesiredDirection((retreatTarget - player.GlobalPosition).Normalized());
            }

            return;
        }

        if (_ball!.Carrier == player)
        {
            var attackGoal = PocketPitchConfig.AttackingGoalCenter(player.TeamSide);
            var shotDistance = player.GlobalPosition.DistanceTo(attackGoal);
            var shootingLaneClear = HasOpenLane(player.GlobalPosition, attackGoal, player.TeamSide, 70f);

            if (shotDistance < 250f && shootingLaneClear)
            {
                AttemptShot(player, 0.78f);
                return;
            }

            if (shotDistance < 340f && player.Role == PlayerRole.Big && shootingLaneClear && _rng.Randf() > 0.3f)
            {
                AttemptShot(player, 0.92f);
                return;
            }

            if (ShouldAiPass(player))
            {
                AttemptPass(player, true);
                return;
            }

            var dribbleTarget = attackGoal + new Vector2((player.SlotIndex - 2) * 35f, player.TeamSide == TeamSide.Home ? 90f : -90f);
            player.SetDesiredDirection((dribbleTarget - player.GlobalPosition).Normalized());
            return;
        }

        if (TeamInPossession(player.TeamSide))
        {
            var supportTarget = GetSupportTarget(player);
            player.SetDesiredDirection((supportTarget - player.GlobalPosition).Normalized());
            return;
        }

        if (_ball.Carrier == null)
        {
            if (isPressingPlayer)
            {
                player.SetDesiredDirection((_ball.GlobalPosition - player.GlobalPosition).Normalized());
            }
            else
            {
                var target = player.SpawnAnchor.Lerp(_ball.GlobalPosition, 0.28f);
                player.SetDesiredDirection((target - player.GlobalPosition).Normalized());
            }

            return;
        }

        if (isPressingPlayer)
        {
            player.SetDesiredDirection((_ball.GlobalPosition - player.GlobalPosition).Normalized());
            if (_ball.Carrier is PlayerController opponent && opponent.TeamSide != player.TeamSide)
            {
                var distance = player.GlobalPosition.DistanceTo(opponent.GlobalPosition);
                if (distance < player.Radius + opponent.Radius + player.Stats.TackleReach)
                {
                    if (distance < 46f)
                    {
                        AttemptStandTackle(player);
                    }
                    else if (distance < 78f && _rng.Randf() > 0.92f)
                    {
                        AttemptSlideTackle(player, opponent.GlobalPosition - player.GlobalPosition);
                    }
                }
            }
        }
        else
        {
            var target = GetDefensiveTarget(player);
            player.SetDesiredDirection((target - player.GlobalPosition).Normalized());
        }
    }

    private void SimulateActors(double delta)
    {
        foreach (var player in _homePlayers)
        {
            player.Simulate(delta, _ball!.Carrier == player);
        }

        foreach (var player in _awayPlayers)
        {
            player.Simulate(delta, _ball!.Carrier == player);
        }

        _homeKeeper!.Simulate(delta, _ball!.GlobalPosition, _ball.Carrier == _homeKeeper);
        _awayKeeper!.Simulate(delta, _ball.GlobalPosition, _ball.Carrier == _awayKeeper);
    }

    private void ResolveSeparation()
    {
        var agents = new List<IPitchAgent>();
        agents.AddRange(_homePlayers);
        agents.AddRange(_awayPlayers);
        agents.Add(_homeKeeper!);
        agents.Add(_awayKeeper!);

        for (var i = 0; i < agents.Count; i++)
        {
            for (var j = i + 1; j < agents.Count; j++)
            {
                var a = agents[i];
                var b = agents[j];
                var delta = b.GlobalPosition - a.GlobalPosition;
                var distance = delta.Length();
                var minimumDistance = a.Radius + b.Radius - 3f;
                if (distance <= 0.001f || distance >= minimumDistance)
                {
                    continue;
                }

                var push = (minimumDistance - distance) * 0.5f;
                var normal = delta / distance;
                a.ApplySeparation(-normal * push);
                b.ApplySeparation(normal * push);
            }
        }
    }

    private void ClampPlayers()
    {
        var fieldRect = PocketPitchConfig.FieldRect.Grow(-10f);
        foreach (var player in _homePlayers)
        {
            player.ClampToField(fieldRect);
        }

        foreach (var player in _awayPlayers)
        {
            player.ClampToField(fieldRect);
        }
    }

    private void EnforceGoalkeeperProtection()
    {
        if (_ball!.Carrier == _homeKeeper)
        {
            EnforceGoalkeeperProtection(_homeKeeper!, _awayPlayers);
        }

        if (_ball.Carrier == _awayKeeper)
        {
            EnforceGoalkeeperProtection(_awayKeeper!, _homePlayers);
        }
    }

    private static void EnforceGoalkeeperProtection(GoalkeeperAI keeper, List<PlayerController> opponents)
    {
        if (keeper.GetParent() == null)
        {
            return;
        }

        var protectedArea = PocketPitchConfig.PenaltyAreaRect(keeper.TeamSide).Grow(24f);
        if (keeper.TeamSide == TeamSide.Home)
        {
            foreach (var opponent in opponents)
            {
                if (!protectedArea.HasPoint(opponent.GlobalPosition))
                {
                    continue;
                }

                opponent.GlobalPosition = new Vector2(
                    opponent.GlobalPosition.X,
                    protectedArea.Position.Y - opponent.Radius - 8f);
            }
        }
        else
        {
            foreach (var opponent in opponents)
            {
                if (!protectedArea.HasPoint(opponent.GlobalPosition))
                {
                    continue;
                }

                opponent.GlobalPosition = new Vector2(
                    opponent.GlobalPosition.X,
                    protectedArea.End.Y + opponent.Radius + 8f);
            }
        }
    }

    private void CheckSlideTackles()
    {
        foreach (var tackler in _homePlayers)
        {
            ResolveSlideTackle(tackler);
        }

        foreach (var tackler in _awayPlayers)
        {
            ResolveSlideTackle(tackler);
        }
    }

    private void ResolveSlideTackle(PlayerController tackler)
    {
        if (!tackler.SlideHitAvailable || _ball!.Carrier is not PlayerController carrier || carrier.TeamSide == tackler.TeamSide)
        {
            return;
        }

        var distance = tackler.GlobalPosition.DistanceTo(carrier.GlobalPosition);
        var directionToCarrier = carrier.GlobalPosition - tackler.GlobalPosition;
        if (distance > tackler.Radius + carrier.Radius + 16f)
        {
            return;
        }

        if (directionToCarrier.LengthSquared() > 0.01f && tackler.FacingDirection.Dot(directionToCarrier.Normalized()) < -0.2f)
        {
            return;
        }

        tackler.ConsumeSlideHit();
        ResolveTackle(tackler, carrier, true);
    }

    private bool AttemptStandTackle(PlayerController tackler)
    {
        if (!tackler.CanAct || _ball!.Carrier is not PlayerController carrier || carrier.TeamSide == tackler.TeamSide)
        {
            return false;
        }

        var directionToCarrier = carrier.GlobalPosition - tackler.GlobalPosition;
        var distance = directionToCarrier.Length();
        if (distance > tackler.Radius + carrier.Radius + tackler.Stats.TackleReach)
        {
            return false;
        }

        if (directionToCarrier.LengthSquared() > 0.01f && tackler.FacingDirection.Dot(directionToCarrier.Normalized()) < 0.15f)
        {
            return false;
        }

        tackler.FlashAction();
        tackler.ApplyRecovery(0.15f);
        return ResolveTackle(tackler, carrier, false);
    }

    private bool AttemptSlideTackle(PlayerController tackler, Vector2 intendedDirection)
    {
        var direction = intendedDirection.LengthSquared() > 0.01f
            ? intendedDirection.Normalized()
            : (_ball!.GlobalPosition - tackler.GlobalPosition).Normalized();
        return tackler.BeginSlide(direction);
    }

    private bool ResolveTackle(PlayerController tackler, PlayerController carrier, bool sliding)
    {
        var tackleValue = tackler.Stats.TackleStrength + (sliding ? 1.25f : 0f) + _rng.Randf();
        var controlValue = carrier.Stats.BallRetention + _rng.Randf();
        if (tackleValue < controlValue)
        {
            if (sliding)
            {
                tackler.ApplyRecovery(0.48f);
            }

            return false;
        }

        var looseDirection = (carrier.GlobalPosition - tackler.GlobalPosition).Normalized();
        if (looseDirection == Vector2.Zero)
        {
            looseDirection = tackler.FacingDirection;
        }

        carrier.ApplyRecovery(sliding ? 0.34f : 0.18f);
        _ball!.KickFrom(carrier, looseDirection * (sliding ? 480f : 320f), 0.08f);
        SetStatus(sliding ? "Crunching tackle!" : "Ball won!", 0.45f);
        return true;
    }

    private bool AttemptPass(PlayerController passer, bool isAi)
    {
        if (_ball!.Carrier != passer)
        {
            return false;
        }

        var teammates = passer.TeamSide == TeamSide.Home ? _homePlayers : _awayPlayers;
        PlayerController? target = null;
        var bestScore = float.MinValue;
        var moveInput = passer == _controlledPlayer ? ReadMoveInput() : passer.FacingDirection;
        if (moveInput == Vector2.Zero)
        {
            moveInput = passer.FacingDirection;
        }

        foreach (var teammate in teammates)
        {
            if (teammate == passer)
            {
                continue;
            }

            var toMate = teammate.GlobalPosition - passer.GlobalPosition;
            var distance = toMate.Length();
            if (distance < 10f)
            {
                continue;
            }

            var direction = toMate / distance;
            var directionalWeight = Mathf.Max(-0.4f, moveInput.Dot(direction));
            var progressWeight = passer.TeamSide == TeamSide.Home ? -teammate.GlobalPosition.Y : teammate.GlobalPosition.Y;
            var laneBonus = HasOpenLane(passer.GlobalPosition, teammate.GlobalPosition, passer.TeamSide, 55f) ? 120f : -140f;
            var score = (directionalWeight * 240f) - distance + (progressWeight * 0.18f) + laneBonus;
            if (!isAi && directionalWeight < -0.2f)
            {
                score -= 160f;
            }

            if (score > bestScore)
            {
                bestScore = score;
                target = teammate;
            }
        }

        if (target == null)
        {
            return false;
        }

        var rawDirection = (target.GlobalPosition - passer.GlobalPosition).Normalized();
        var scatter = Mathf.DegToRad(_rng.RandfRange(-passer.Stats.PassScatterDegrees, passer.Stats.PassScatterDegrees));
        var finalDirection = rawDirection.Rotated(scatter);
        _ball.KickFrom(passer, finalDirection * passer.Stats.PassSpeed, 0.06f);
        passer.FlashAction();
        passer.ApplyRecovery(0.08f);
        return true;
    }

    private bool AttemptShot(PlayerController shooter, float powerScale)
    {
        if (_ball!.Carrier != shooter)
        {
            return false;
        }

        var targetGoal = PocketPitchConfig.AttackingGoalCenter(shooter.TeamSide);
        var desired = targetGoal - shooter.GlobalPosition;
        if (shooter == _controlledPlayer)
        {
            var input = ReadMoveInput();
            if (input != Vector2.Zero)
            {
                desired = (input * 250f) + (targetGoal - shooter.GlobalPosition);
            }
        }

        var shotDirection = desired.Normalized();
        var scatter = Mathf.DegToRad(_rng.RandfRange(-shooter.Stats.ShotScatterDegrees, shooter.Stats.ShotScatterDegrees));
        shotDirection = shotDirection.Rotated(scatter);
        _ball.KickFrom(shooter, shotDirection * shooter.Stats.ShotSpeed * Mathf.Clamp(powerScale, 0.25f, 1.12f), 0.12f);
        shooter.FlashAction();
        shooter.ApplyRecovery(0.12f);
        return true;
    }

    private void ProcessGoalkeepers()
    {
        TryGoalkeeperClaim(_homeKeeper!);
        TryGoalkeeperClaim(_awayKeeper!);

        if (_ball!.Carrier == _homeKeeper && (_homeKeeper!.ReadyToDistribute || IsKeeperUnderPressure(_homeKeeper, _awayPlayers)))
        {
            GoalkeeperDistribute(_homeKeeper, _homePlayers);
        }

        if (_ball.Carrier == _awayKeeper && (_awayKeeper!.ReadyToDistribute || IsKeeperUnderPressure(_awayKeeper, _homePlayers)))
        {
            GoalkeeperDistribute(_awayKeeper, _awayPlayers);
        }
    }

    private void TryGoalkeeperClaim(GoalkeeperAI keeper)
    {
        var activeZone = PocketPitchConfig.GoalkeeperRoamRect(keeper.TeamSide).Grow(24f);
        if (!activeZone.HasPoint(_ball!.GlobalPosition))
        {
            return;
        }

        if (!_ball.CanBeClaimedBy(keeper))
        {
            return;
        }

        if (_ball.Carrier == null)
        {
            if (_ball.Speed > GoalkeeperClaimSpeedThreshold)
            {
                return;
            }

            if (keeper.GlobalPosition.DistanceTo(_ball.GlobalPosition) <= keeper.PickupRadius + PocketPitchConfig.BallRadius)
            {
                _ball.AttachTo(keeper);
                keeper.CaptureBall();
                SetStatus("Saved!", 0.5f);
            }

            return;
        }

        if (_ball.Carrier is PlayerController carrier &&
            carrier.TeamSide != keeper.TeamSide &&
            keeper.GlobalPosition.DistanceTo(carrier.GlobalPosition) <= keeper.Radius + carrier.Radius + 10f)
        {
            _ball.AttachTo(keeper);
            keeper.CaptureBall();
            carrier.ApplyRecovery(0.3f);
            SetStatus("Keeper smothers it!", 0.55f);
        }
    }

    private void GoalkeeperDistribute(GoalkeeperAI keeper, List<PlayerController> teammates)
    {
        PlayerController? target = null;
        var bestScore = float.MinValue;
        foreach (var teammate in teammates)
        {
            var attackValue = keeper.TeamSide == TeamSide.Home ? -teammate.GlobalPosition.Y : teammate.GlobalPosition.Y;
            var spacing = Mathf.Abs(teammate.GlobalPosition.X);
            var laneBonus = HasOpenLane(keeper.GlobalPosition, teammate.GlobalPosition, keeper.TeamSide, 50f) ? 120f : -60f;
            var score = attackValue + (spacing * 0.3f) + laneBonus;
            if (score > bestScore)
            {
                bestScore = score;
                target = teammate;
            }
        }

        if (target == null)
        {
            return;
        }

        var direction = (target.GlobalPosition - keeper.GlobalPosition).Normalized();
        _ball!.KickFrom(keeper, direction * 620f, 0.2f);
    }

    private bool CheckGoalScored()
    {
        var ballPosition = _ball!.GlobalPosition;
        var goalHalfWidth = PocketPitchConfig.GoalWidth * 0.5f;
        var topGoalLine = PocketPitchConfig.FieldRect.Position.Y;
        var bottomGoalLine = PocketPitchConfig.FieldRect.End.Y;

        if (Mathf.Abs(ballPosition.X) <= goalHalfWidth && ballPosition.Y < topGoalLine - 2f)
        {
            RegisterGoal(TeamSide.Home);
            return true;
        }

        if (Mathf.Abs(ballPosition.X) <= goalHalfWidth && ballPosition.Y > bottomGoalLine + 2f)
        {
            RegisterGoal(TeamSide.Away);
            return true;
        }

        return false;
    }

    private void KeepBallInPlay()
    {
        if (_ball!.Carrier != null)
        {
            return;
        }

        var fieldRect = PocketPitchConfig.FieldRect;
        var goalHalfWidth = PocketPitchConfig.GoalWidth * 0.5f;
        if (_ball.GlobalPosition.X < fieldRect.Position.X + PocketPitchConfig.BallRadius)
        {
            _ball.BounceX(fieldRect.Position.X + PocketPitchConfig.BallRadius);
        }
        else if (_ball.GlobalPosition.X > fieldRect.End.X - PocketPitchConfig.BallRadius)
        {
            _ball.BounceX(fieldRect.End.X - PocketPitchConfig.BallRadius);
        }

        var inGoalMouth = Mathf.Abs(_ball.GlobalPosition.X) <= goalHalfWidth;
        if (!inGoalMouth && _ball.GlobalPosition.Y < fieldRect.Position.Y + PocketPitchConfig.BallRadius)
        {
            _ball.BounceY(fieldRect.Position.Y + PocketPitchConfig.BallRadius);
        }
        else if (!inGoalMouth && _ball.GlobalPosition.Y > fieldRect.End.Y - PocketPitchConfig.BallRadius)
        {
            _ball.BounceY(fieldRect.End.Y - PocketPitchConfig.BallRadius);
        }
    }

    private void ClaimLooseBall()
    {
        if (_ball!.Carrier != null)
        {
            return;
        }

        var agents = new List<IBallCarrier>();
        agents.AddRange(_homePlayers);
        agents.AddRange(_awayPlayers);
        agents.Add(_homeKeeper!);
        agents.Add(_awayKeeper!);

        IBallCarrier? bestCandidate = null;
        var bestDistance = float.MaxValue;
        foreach (var candidate in agents)
        {
            if (!candidate.CanReceiveBall || !_ball.CanBeClaimedBy((Node)candidate))
            {
                continue;
            }

            var distance = candidate.GlobalPosition.DistanceTo(_ball.GlobalPosition);
            if (distance > candidate.PickupRadius + PocketPitchConfig.BallRadius)
            {
                continue;
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestCandidate = candidate;
            }
        }

        if (bestCandidate == null)
        {
            return;
        }

        _ball.AttachTo(bestCandidate);
        if (bestCandidate is GoalkeeperAI keeper)
        {
            keeper.CaptureBall();
        }
    }

    private void RegisterGoal(TeamSide scoringSide)
    {
        if (scoringSide == TeamSide.Home)
        {
            _homeScore++;
            _nextKickoffTeam = TeamSide.Away;
            SetStatus($"{_homeTeam.Name} score!", PocketPitchConfig.GoalFreezeSeconds);
        }
        else
        {
            _awayScore++;
            _nextKickoffTeam = TeamSide.Home;
            SetStatus($"{_awayTeam.Name} score!", PocketPitchConfig.GoalFreezeSeconds);
        }

        _phase = MatchPhase.GoalScored;
        _phaseTimer = PocketPitchConfig.GoalFreezeSeconds;
        _ball!.Stop();
    }

    private void FinishMatch()
    {
        _phase = MatchPhase.FullTime;
        var summary = $"{_homeTeam.Name}  {_homeScore} - {_awayScore}  {_awayTeam.Name}";
        _hud!.ShowFullTime(summary);
        SetStatus("Full time", 1000f);
    }

    private void TogglePause()
    {
        if (_phase == MatchPhase.Paused)
        {
            _phase = _phaseBeforePause;
            SetStatus(string.Empty, 0f);
            return;
        }

        _phaseBeforePause = _phase;
        _phase = MatchPhase.Paused;
        SetStatus("Paused", 1000f);
    }

    private bool TeamInPossession(TeamSide side)
    {
        return _ball!.Carrier != null && _ball.Carrier.TeamSide == side;
    }

    private PlayerController GetClosestPlayerToBall(List<PlayerController> players)
    {
        var best = players[0];
        var bestDistance = float.MaxValue;
        foreach (var player in players)
        {
            var distance = player.GlobalPosition.DistanceSquaredTo(_ball!.GlobalPosition);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = player;
            }
        }

        return best;
    }

    private Vector2 GetSupportTarget(PlayerController player)
    {
        var anchor = player.SpawnAnchor;
        var ballInfluence = _ball!.GlobalPosition;
        var attackSign = player.TeamSide == TeamSide.Home ? -1f : 1f;
        var target = new Vector2(
            Mathf.Clamp((anchor.X * 0.65f) + (ballInfluence.X * 0.35f), -PocketPitchConfig.FieldWidth * 0.42f, PocketPitchConfig.FieldWidth * 0.42f),
            Mathf.Clamp((anchor.Y * 0.55f) + (ballInfluence.Y * 0.28f) + (attackSign * 80f), -PocketPitchConfig.FieldHeight * 0.38f, PocketPitchConfig.FieldHeight * 0.38f));

        if (player.Role == PlayerRole.Small)
        {
            target += new Vector2(player.SlotIndex < 2 ? -70f : 70f, attackSign * 18f);
        }

        return target;
    }

    private Vector2 GetDefensiveTarget(PlayerController player)
    {
        var anchor = player.SpawnAnchor;
        var goal = PocketPitchConfig.GoalCenter(player.TeamSide);
        var ballPressurePoint = _ball!.GlobalPosition.Lerp(goal, 0.35f);
        return new Vector2(
            Mathf.Lerp(anchor.X, ballPressurePoint.X, 0.45f),
            Mathf.Lerp(anchor.Y, ballPressurePoint.Y, 0.58f));
    }

    private Vector2 GetGoalkeeperRespectTarget(TeamSide keeperSide, PlayerController player)
    {
        var protectedArea = PocketPitchConfig.PenaltyAreaRect(keeperSide);
        var targetY = keeperSide == TeamSide.Home
            ? protectedArea.Position.Y - 42f
            : protectedArea.End.Y + 42f;
        return new Vector2(player.SpawnAnchor.X, targetY);
    }

    private static bool IsKeeperUnderPressure(GoalkeeperAI keeper, List<PlayerController> opponents)
    {
        foreach (var opponent in opponents)
        {
            if (opponent.GlobalPosition.DistanceTo(keeper.GlobalPosition) <= GoalkeeperPressureDistance)
            {
                return true;
            }
        }

        return false;
    }

    private bool ShouldAiPass(PlayerController player)
    {
        var distanceToGoal = player.GlobalPosition.DistanceTo(PocketPitchConfig.AttackingGoalCenter(player.TeamSide));
        if (distanceToGoal > 280f && !HasOpenLane(player.GlobalPosition, PocketPitchConfig.AttackingGoalCenter(player.TeamSide), player.TeamSide, 75f))
        {
            return true;
        }

        return _rng.Randf() > 0.985f;
    }

    private bool HasOpenLane(Vector2 from, Vector2 to, TeamSide passingTeam, float laneWidth)
    {
        var opponents = passingTeam == TeamSide.Home ? _awayPlayers : _homePlayers;
        foreach (var opponent in opponents)
        {
            if (DistanceToSegment(opponent.GlobalPosition, from, to) < laneWidth)
            {
                return false;
            }
        }

        return true;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
    {
        var line = segmentEnd - segmentStart;
        if (line.LengthSquared() <= 0.0001f)
        {
            return point.DistanceTo(segmentStart);
        }

        var t = Mathf.Clamp((point - segmentStart).Dot(line) / line.LengthSquared(), 0f, 1f);
        var projection = segmentStart + (line * t);
        return point.DistanceTo(projection);
    }

    private Vector2 ReadMoveInput()
    {
        return Input.GetVector("move_left", "move_right", "move_up", "move_down");
    }

    private void SetStatus(string text, float duration)
    {
        _statusTimer = duration;
        _hud!.SetStatus(text);
    }

    private void UpdateStatus(double delta)
    {
        if (_statusTimer <= 0f)
        {
            return;
        }

        _statusTimer = Mathf.Max(0f, _statusTimer - (float)delta);
        if (_statusTimer <= 0f && _phase == MatchPhase.InPlay)
        {
            _hud!.SetStatus(string.Empty);
        }
    }

    private void UpdateHud()
    {
        _hud!.UpdateScore(_homeScore, _awayScore, _homeTeam.Name, _awayTeam.Name);
        _hud.UpdateTimer(_matchTimeRemaining);
        _hud.SetShotCharge(0f);
    }
}
