using System;
using System.Collections.Generic;
using Godot;

public partial class MatchManager : Node2D
{
    private const float GoalkeeperClaimSpeedThreshold = 760f;
    private const float GoalkeeperPressureDistance = 84f;
    private const float RecentAiReturnPassWindow = 1.15f;
    public event Action? ReturnToMenuRequested;
    public event Action<MatchResult>? MatchCompleted;

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
    private PlayerController? _homeControlledPlayer;
    private PlayerController? _awayControlledPlayer;
    private TeamDefinition _homeTeam = PocketPitchConfig.DefaultHomeTeam.Clone();
    private TeamDefinition _awayTeam = PocketPitchConfig.AwayPresets[0].Clone();
    private MatchSettings _settings = new(
        PocketPitchConfig.DefaultHomeTeam,
        PocketPitchConfig.AwayPresets[0],
        true,
        false);
    private MatchPhase _phase = MatchPhase.PreKickoff;
    private MatchPhase _phaseBeforePause = MatchPhase.InPlay;
    private TeamSide _nextKickoffTeam = TeamSide.Home;
    private float _phaseTimer;
    private float _matchTimeRemaining = PocketPitchConfig.MatchLengthSeconds;
    private float _statusTimer;
    private float _recentAiPassTimer;
    private int _homeScore;
    private int _awayScore;
    private MatchResult? _lastMatchResult;
    private PlayerController? _recentAiPassSource;
    private PlayerController? _recentAiPassTarget;

    public void Configure(MatchSettings settings)
    {
        _settings = settings;
        _homeTeam = settings.HomeTeam.Clone();
        _awayTeam = settings.AwayTeam.Clone();
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

        _hud.PrimaryActionRequested += () =>
        {
            if (_lastMatchResult != null)
            {
                MatchCompleted?.Invoke(_lastMatchResult);
            }
        };
        _hud.MenuRequested += () => ReturnToMenuRequested?.Invoke();
        _hud.ConfigureEndActions(_settings.PrimaryActionText, _settings.SecondaryActionText);
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
        if (_recentAiPassTimer > 0f)
        {
            _recentAiPassTimer = Mathf.Max(0f, _recentAiPassTimer - (float)delta);
            if (_recentAiPassTimer <= 0f)
            {
                _recentAiPassSource = null;
                _recentAiPassTarget = null;
            }
        }

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

        UpdateControlledPlayers();
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

        var squadSize = Mathf.Min(
            PocketPitchConfig.OutfieldPlayerCount,
            Mathf.Min(_homeTeam.Roles.Length, Mathf.Min(_awayTeam.Roles.Length, Mathf.Min(PocketPitchConfig.HomeAnchors.Length, PocketPitchConfig.AwayAnchors.Length))));
        for (var i = 0; i < squadSize; i++)
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
        _lastMatchResult = null;
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
            if (kickoffTeam == TeamSide.Home && i == PocketPitchConfig.StrikerSlot)
            {
                homePosition = new Vector2(0f, 88f);
            }
            else if (kickoffTeam == TeamSide.Home && i == PocketPitchConfig.SupportStrikerSlot)
            {
                homePosition = new Vector2(0f, 148f);
            }

            _homePlayers[i].ResetForPosition(homePosition, Vector2.Up);
        }

        for (var i = 0; i < _awayPlayers.Count; i++)
        {
            var awayPosition = PocketPitchConfig.AwayAnchors[i];
            if (kickoffTeam == TeamSide.Away && i == PocketPitchConfig.StrikerSlot)
            {
                awayPosition = new Vector2(0f, -88f);
            }
            else if (kickoffTeam == TeamSide.Away && i == PocketPitchConfig.SupportStrikerSlot)
            {
                awayPosition = new Vector2(0f, -148f);
            }

            _awayPlayers[i].ResetForPosition(awayPosition, Vector2.Down);
        }

        _homeKeeper!.ResetForKickoff();
        _awayKeeper!.ResetForKickoff();

        var kickoffPlayer = kickoffTeam == TeamSide.Home ? _homePlayers[PocketPitchConfig.StrikerSlot] : _awayPlayers[PocketPitchConfig.StrikerSlot];
        _ball!.AttachTo(kickoffPlayer);
        UpdateControlledPlayers();
        SetStatus(kickoffTeam == TeamSide.Home ? "Blue kick off" : "Orange kick off", PocketPitchConfig.KickoffDelaySeconds);
        UpdateHud();
    }

    private void UpdateControlledPlayers()
    {
        if (_settings.HomeHumanControlled)
        {
            _homeControlledPlayer = ResolveControlledPlayer(TeamSide.Home, _homePlayers);
        }

        if (_settings.AwayHumanControlled)
        {
            _awayControlledPlayer = ResolveControlledPlayer(TeamSide.Away, _awayPlayers);
        }

        foreach (var player in _homePlayers)
        {
            player.SetControlState(_settings.HomeHumanControlled && player == _homeControlledPlayer, _ball!.Carrier == player);
        }

        foreach (var player in _awayPlayers)
        {
            player.SetControlState(_settings.AwayHumanControlled && player == _awayControlledPlayer, _ball!.Carrier == player);
        }
    }

    private void HandleInput(double delta)
    {
        foreach (var player in _homePlayers)
        {
            if (!_settings.HomeHumanControlled || player != _homeControlledPlayer)
            {
                player.SetDesiredDirection(Vector2.Zero);
            }
        }

        foreach (var player in _awayPlayers)
        {
            if (!_settings.AwayHumanControlled || player != _awayControlledPlayer)
            {
                player.SetDesiredDirection(Vector2.Zero);
            }
        }

        if (_settings.HomeHumanControlled && _homeControlledPlayer != null)
        {
            HandleHumanInput(TeamSide.Home, _homeControlledPlayer, ReadMoveInput(TeamSide.Home));
        }

        if (_settings.AwayHumanControlled && _awayControlledPlayer != null)
        {
            HandleHumanInput(TeamSide.Away, _awayControlledPlayer, ReadMoveInput(TeamSide.Away));
        }
    }

    private PlayerController? ResolveControlledPlayer(TeamSide teamSide, List<PlayerController> teamPlayers)
    {
        if (_ball!.Carrier is PlayerController owner && owner.TeamSide == teamSide)
        {
            return owner;
        }

        var closestDistance = float.MaxValue;
        PlayerController? closest = null;
        foreach (var player in teamPlayers)
        {
            var distance = player.GlobalPosition.DistanceSquaredTo(_ball.GlobalPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = player;
            }
        }

        return closest;
    }

    private void HandleHumanInput(TeamSide teamSide, PlayerController controlledPlayer, Vector2 moveInput)
    {
        controlledPlayer.SetDesiredDirection(moveInput);
        var humanHasBall = _ball!.Carrier == controlledPlayer;
        var actionA = GetActionAName(teamSide);
        var actionB = GetActionBName(teamSide);

        if (humanHasBall)
        {
            if (Input.IsActionJustPressed(actionA))
            {
                AttemptPass(controlledPlayer, false, moveInput);
            }

            if (Input.IsActionJustPressed(actionB))
            {
                AttemptShot(controlledPlayer, 1.08f, moveInput);
            }
        }
        else
        {
            if (Input.IsActionJustPressed(actionA))
            {
                AttemptStandTackle(controlledPlayer);
            }

            if (Input.IsActionJustPressed(actionB))
            {
                AttemptSlideTackle(controlledPlayer, moveInput);
            }
        }
    }

    private void RunAi()
    {
        var homePressing = GetClosestPlayerToBall(_homePlayers);
        var awayPressing = GetClosestPlayerToBall(_awayPlayers);

        foreach (var player in _homePlayers)
        {
            if (_settings.HomeHumanControlled && player == _homeControlledPlayer)
            {
                continue;
            }

            UpdateAiPlayer(player, player == homePressing);
        }

        foreach (var player in _awayPlayers)
        {
            if (_settings.AwayHumanControlled && player == _awayControlledPlayer)
            {
                continue;
            }

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
            var shootingLaneClear = HasOpenLane(player.GlobalPosition, attackGoal, player.TeamSide, 66f);
            var pressure = GetOpponentPressure(player, 150f);
            var supportRunnerAhead = CountSupportingRunnersAhead(player);

            if (ShouldAiShoot(player, shotDistance, shootingLaneClear, pressure))
            {
                var powerScale = player.Role == PlayerRole.Big
                    ? (shotDistance < 300f ? 0.98f : 0.92f)
                    : (shotDistance < 215f ? 0.88f : 0.8f);
                AttemptShot(player, powerScale, player.FacingDirection);
                return;
            }

            if (ShouldAiPass(player, shotDistance, shootingLaneClear, pressure, supportRunnerAhead))
            {
                AttemptPass(player, true, player.FacingDirection);
                return;
            }

            var dribbleTarget = GetAiDribbleTarget(player, attackGoal, pressure);
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

        if (!CanSlideTackleRole(tackler.Role, carrier.Role))
        {
            tackler.ConsumeSlideHit();
            tackler.ApplyRecovery(0.32f);
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
        var controlMultiplier = sliding ? 1f : 1.12f;
        var controlValue = (carrier.Stats.BallRetention * controlMultiplier) + _rng.Randf() + (sliding ? 0f : _rng.Randf() * 0.35f);
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

        if (sliding)
        {
            var impactDirection = looseDirection;
            var impactStrength = tackler.Role switch
            {
                PlayerRole.Big => 760f,
                PlayerRole.Medium => 640f,
                _ => 520f
            };
            var looseSpeed = tackler.Role switch
            {
                PlayerRole.Big => 760f,
                PlayerRole.Medium => 680f,
                _ => 600f
            };
            var looseScatter = Mathf.DegToRad(_rng.RandfRange(-26f, 26f));
            var looseBallDirection = impactDirection.Rotated(looseScatter);
            carrier.ApplyImpact(impactDirection * impactStrength, 0.52f);
            _ball!.KickFrom(carrier, looseBallDirection * looseSpeed, 0.12f);
            _ball.LockClaimsAgainst(tackler, 0.20f);
            SetStatus("Crunching tackle!", 0.45f);
        }
        else
        {
            carrier.ApplyRecovery(0.2f);
            _ball!.KickFrom(carrier, looseDirection * 320f, 0.08f);
            SetStatus("Ball won!", 0.45f);
        }

        return true;
    }

    private bool AttemptPass(PlayerController passer, bool isAi, Vector2 directionalInput)
    {
        if (_ball!.Carrier != passer)
        {
            return false;
        }

        var teammates = passer.TeamSide == TeamSide.Home ? _homePlayers : _awayPlayers;
        PlayerController? target = null;
        var bestScore = float.MinValue;
        var moveInput = directionalInput;
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
            var spacingBonus = Mathf.Clamp(Mathf.Abs(teammate.GlobalPosition.X - passer.GlobalPosition.X) * 0.22f, 0f, 95f);
            var shortPassPenalty = distance < 150f ? 120f : 0f;
            var backtrackPenalty = TeamProgressValue(teammate.GlobalPosition, passer.TeamSide) < TeamProgressValue(passer.GlobalPosition, passer.TeamSide) - 35f ? 90f : 0f;
            var returnPassPenalty = isAi && _recentAiPassTimer > 0f && passer == _recentAiPassTarget && teammate == _recentAiPassSource ? 220f : 0f;
            var score = (directionalWeight * 240f) - distance + (progressWeight * 0.22f) + laneBonus + spacingBonus - shortPassPenalty - backtrackPenalty - returnPassPenalty;
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
        if (isAi)
        {
            _recentAiPassSource = passer;
            _recentAiPassTarget = target;
            _recentAiPassTimer = RecentAiReturnPassWindow;
        }
        return true;
    }

    private bool AttemptShot(PlayerController shooter, float powerScale, Vector2 directionalInput)
    {
        if (_ball!.Carrier != shooter)
        {
            return false;
        }

        var targetGoal = PocketPitchConfig.AttackingGoalCenter(shooter.TeamSide);
        var desired = targetGoal - shooter.GlobalPosition;
        if (directionalInput != Vector2.Zero)
        {
            desired = (directionalInput * 250f) + (targetGoal - shooter.GlobalPosition);
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
        _lastMatchResult = new MatchResult(_homeTeam, _awayTeam, _homeScore, _awayScore);
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
        var ball = _ball!;
        var reference = TeamInPossession(player.TeamSide) && ball.Carrier is PlayerController carrier && carrier.TeamSide == player.TeamSide
            ? carrier.GlobalPosition
            : ball.GlobalPosition;
        var attackSign = player.TeamSide == TeamSide.Home ? -1f : 1f;
        var possessionPush = TeamInPossession(player.TeamSide) ? 1f : 0f;
        var roleWidth = GetSlotWidthBias(player.SlotIndex);
        var lineAdvance = GetSlotAttackAdvance(player.SlotIndex);
        var progress = Mathf.Clamp(TeamProgressValue(reference, player.TeamSide), -240f, 640f);
        var horizontalFollow = GetSlotHorizontalFollow(player.SlotIndex) + (0.08f * possessionPush);
        var targetWidth = Mathf.Clamp((roleWidth * 0.92f) + (reference.X * 0.34f), -PocketPitchConfig.FieldWidth * 0.40f, PocketPitchConfig.FieldWidth * 0.40f);
        var target = new Vector2(
            Mathf.Lerp(anchor.X, targetWidth, horizontalFollow),
            Mathf.Clamp(
                anchor.Y + (attackSign * progress * lineAdvance),
                -PocketPitchConfig.FieldHeight * 0.46f,
                PocketPitchConfig.FieldHeight * 0.46f));

        if (player.Role == PlayerRole.Small)
        {
            target += new Vector2(Mathf.Sign(roleWidth) * 85f, attackSign * 36f);
        }
        else if (player.Role == PlayerRole.Big)
        {
            target += new Vector2(0f, -attackSign * 14f);
        }

        return target;
    }

    private Vector2 GetDefensiveTarget(PlayerController player)
    {
        var anchor = player.SpawnAnchor;
        var goal = PocketPitchConfig.GoalCenter(player.TeamSide);
        var ballPressurePoint = _ball!.GlobalPosition.Lerp(goal, 0.28f);
        var roleWidth = GetSlotWidthBias(player.SlotIndex);
        var lineDrop = GetSlotDefenseCompression(player.SlotIndex);
        var centralBias = new Vector2(
            Mathf.Lerp(roleWidth, _ball.GlobalPosition.X * 0.26f, 0.24f),
            Mathf.Lerp(anchor.Y, ballPressurePoint.Y, lineDrop));
        return new Vector2(
            Mathf.Clamp(centralBias.X, -PocketPitchConfig.FieldWidth * 0.38f, PocketPitchConfig.FieldWidth * 0.38f),
            Mathf.Clamp(centralBias.Y, -PocketPitchConfig.FieldHeight * 0.44f, PocketPitchConfig.FieldHeight * 0.44f));
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

    private bool ShouldAiPass(PlayerController player, float distanceToGoal, bool shootingLaneClear, float pressure, int supportRunnerAhead)
    {
        var blockedShot = distanceToGoal > 310f && !shootingLaneClear;
        var underPressure = pressure > 0.62f;
        var isolated = supportRunnerAhead == 0 && distanceToGoal > 220f;
        if (underPressure && distanceToGoal > 165f)
        {
            return true;
        }

        if (blockedShot && supportRunnerAhead > 0 && _rng.Randf() > (player.Role == PlayerRole.Small ? 0.72f : 0.84f))
        {
            return true;
        }

        if (isolated)
        {
            return false;
        }

        return pressure > 0.58f && _rng.Randf() > 0.97f;
    }

    private bool ShouldAiShoot(PlayerController player, float shotDistance, bool shootingLaneClear, float pressure)
    {
        if (!shootingLaneClear)
        {
            return false;
        }

        if (player.Role == PlayerRole.Big)
        {
            return shotDistance < 470f || (shotDistance < 600f && pressure > 0.14f && _rng.Randf() > 0.28f);
        }

        if (player.Role == PlayerRole.Medium)
        {
            return shotDistance < 340f || (shotDistance < 440f && pressure > 0.34f && _rng.Randf() > 0.54f);
        }

        return shotDistance < 250f || (shotDistance < 325f && pressure > 0.56f && _rng.Randf() > 0.66f);
    }

    private Vector2 GetAiDribbleTarget(PlayerController player, Vector2 attackGoal, float pressure)
    {
        var attackSign = player.TeamSide == TeamSide.Home ? -1f : 1f;
        var widthBias = GetSlotWidthBias(player.SlotIndex) * 0.48f;
        var inwardBias = -Mathf.Sign(player.GlobalPosition.X) * 180f;
        var candidates = new[]
        {
            attackGoal + new Vector2(widthBias * 0.55f, attackSign * 130f),
            player.GlobalPosition + new Vector2(inwardBias, attackSign * 165f),
            player.GlobalPosition + new Vector2(widthBias * 0.25f, attackSign * 205f),
            player.GlobalPosition + new Vector2(widthBias, attackSign * 125f),
            player.GlobalPosition + new Vector2(inwardBias * 0.55f, attackSign * 110f)
        };

        var best = candidates[0];
        var bestScore = float.MinValue;
        foreach (var candidate in candidates)
        {
            var clampedCandidate = new Vector2(
                Mathf.Clamp(candidate.X, -PocketPitchConfig.FieldWidth * 0.44f, PocketPitchConfig.FieldWidth * 0.44f),
                Mathf.Clamp(candidate.Y, -PocketPitchConfig.FieldHeight * 0.46f, PocketPitchConfig.FieldHeight * 0.46f));
            var laneBonus = HasOpenLane(player.GlobalPosition, clampedCandidate, player.TeamSide, 58f) ? 140f : -120f;
            var progressBonus = TeamProgressValue(clampedCandidate, player.TeamSide) * 0.18f;
            var crowdPenalty = GetOpponentCrowding(clampedCandidate, player.TeamSide, 125f) * 120f;
            var widthBonus = Mathf.Abs(clampedCandidate.X) * (pressure > 0.45f ? 0.07f : 0.03f);
            var touchlineDistance = (PocketPitchConfig.FieldWidth * 0.5f) - Mathf.Abs(clampedCandidate.X);
            var touchlinePenalty = Mathf.Max(0f, 165f - touchlineDistance) * 1.9f;
            var endlineDistance = player.TeamSide == TeamSide.Home
                ? clampedCandidate.Y - PocketPitchConfig.FieldRect.Position.Y
                : PocketPitchConfig.FieldRect.End.Y - clampedCandidate.Y;
            var cornerPenalty = Mathf.Max(0f, 180f - touchlineDistance) + Mathf.Max(0f, 220f - endlineDistance);
            var score = laneBonus + progressBonus + widthBonus - crowdPenalty - touchlinePenalty - (cornerPenalty * 1.25f);
            if (score > bestScore)
            {
                bestScore = score;
                best = clampedCandidate;
            }
        }

        return best;
    }

    private float GetOpponentPressure(PlayerController player, float radius)
    {
        var opponents = player.TeamSide == TeamSide.Home ? _awayPlayers : _homePlayers;
        var pressure = 0f;
        foreach (var opponent in opponents)
        {
            var distance = player.GlobalPosition.DistanceTo(opponent.GlobalPosition);
            if (distance >= radius)
            {
                continue;
            }

            pressure += 1f - (distance / radius);
        }

        return pressure;
    }

    private float GetOpponentCrowding(Vector2 position, TeamSide attackingSide, float radius)
    {
        var opponents = attackingSide == TeamSide.Home ? _awayPlayers : _homePlayers;
        var crowding = 0f;
        foreach (var opponent in opponents)
        {
            var distance = position.DistanceTo(opponent.GlobalPosition);
            if (distance >= radius)
            {
                continue;
            }

            crowding += 1f - (distance / radius);
        }

        return crowding;
    }

    private int CountSupportingRunnersAhead(PlayerController player)
    {
        var teammates = player.TeamSide == TeamSide.Home ? _homePlayers : _awayPlayers;
        var runners = 0;
        foreach (var teammate in teammates)
        {
            if (teammate == player)
            {
                continue;
            }

            var ahead = player.TeamSide == TeamSide.Home
                ? teammate.GlobalPosition.Y < player.GlobalPosition.Y - 55f
                : teammate.GlobalPosition.Y > player.GlobalPosition.Y + 55f;
            if (ahead)
            {
                runners++;
            }
        }

        return runners;
    }

    private static float TeamProgressValue(Vector2 position, TeamSide attackingSide)
    {
        return attackingSide == TeamSide.Home ? -position.Y : position.Y;
    }

    private static float GetSlotWidthBias(int slotIndex)
    {
        return slotIndex switch
        {
            0 => -260f,
            1 => 260f,
            2 => -210f,
            3 => 210f,
            4 => -165f,
            5 => 165f,
            _ => 0f
        };
    }

    private static float GetSlotAttackAdvance(int slotIndex)
    {
        return slotIndex switch
        {
            0 or 1 => 0.28f,
            2 or 3 => 0.54f,
            4 or 5 => 0.88f,
            _ => 0.5f
        };
    }

    private static float GetSlotDefenseCompression(int slotIndex)
    {
        return slotIndex switch
        {
            0 or 1 => 0.24f,
            2 or 3 => 0.42f,
            4 or 5 => 0.34f,
            _ => 0.35f
        };
    }

    private static float GetSlotHorizontalFollow(int slotIndex)
    {
        return slotIndex switch
        {
            0 or 1 => 0.10f,
            2 or 3 => 0.18f,
            4 or 5 => 0.24f,
            _ => 0.16f
        };
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

    private static bool CanSlideTackleRole(PlayerRole tacklerRole, PlayerRole carrierRole)
    {
        return tacklerRole switch
        {
            PlayerRole.Big => true,
            PlayerRole.Medium => carrierRole != PlayerRole.Big,
            _ => carrierRole == PlayerRole.Small
        };
    }

    private Vector2 ReadMoveInput(TeamSide side)
    {
        return UsesPrimaryInputs(side)
            ? Input.GetVector("move_left", "move_right", "move_up", "move_down")
            : Input.GetVector("p2_move_left", "p2_move_right", "p2_move_up", "p2_move_down");
    }

    private string GetActionAName(TeamSide side)
    {
        return UsesPrimaryInputs(side) ? "action_a" : "p2_action_a";
    }

    private string GetActionBName(TeamSide side)
    {
        return UsesPrimaryInputs(side) ? "action_b" : "p2_action_b";
    }

    private bool UsesPrimaryInputs(TeamSide side)
    {
        var bothHumanControlled = _settings.HomeHumanControlled && _settings.AwayHumanControlled;
        if (!bothHumanControlled)
        {
            return side == TeamSide.Home
                ? _settings.HomeHumanControlled
                : _settings.AwayHumanControlled;
        }

        return side == TeamSide.Home;
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
