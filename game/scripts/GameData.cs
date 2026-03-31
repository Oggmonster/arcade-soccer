using System;
using System.Collections.Generic;
using Godot;

public enum PlayerRole
{
    Big,
    Medium,
    Small
}

public enum TeamSide
{
    Home,
    Away
}

public enum MatchPhase
{
    PreKickoff,
    InPlay,
    GoalScored,
    FullTime,
    Paused
}

public interface IBallCarrier
{
    TeamSide TeamSide { get; }
    Vector2 GlobalPosition { get; }
    Vector2 FacingDirection { get; }
    float BallCarryDistance { get; }
    float PickupRadius { get; }
    bool CanReceiveBall { get; }
}

public interface IPitchAgent : IBallCarrier
{
    float Radius { get; }
    void ApplySeparation(Vector2 offset);
}

public sealed class StatBlock
{
    public PlayerRole Role { get; }
    public int Speed { get; }
    public int Acceleration { get; }
    public int ShotPower { get; }
    public int ShotAccuracy { get; }
    public int PassAccuracy { get; }
    public int TackleStrength { get; }
    public int BallControl { get; }
    public int Recovery { get; }

    public StatBlock(
        PlayerRole role,
        int speed,
        int acceleration,
        int shotPower,
        int shotAccuracy,
        int passAccuracy,
        int tackleStrength,
        int ballControl,
        int recovery)
    {
        Role = role;
        Speed = speed;
        Acceleration = acceleration;
        ShotPower = shotPower;
        ShotAccuracy = shotAccuracy;
        PassAccuracy = passAccuracy;
        TackleStrength = tackleStrength;
        BallControl = ballControl;
        Recovery = recovery;
    }

    public float MoveSpeed => Mathf.Lerp(175f, 315f, Normalized(Speed));
    public float AccelerationUnits => Mathf.Lerp(520f, 980f, Normalized(Acceleration));
    public float ShotSpeed => Mathf.Lerp(860f, 1480f, Normalized(ShotPower));
    public float PassSpeed => Mathf.Lerp(390f, 700f, Normalized(PassAccuracy));
    public float ShotScatterDegrees => Mathf.Lerp(13f, 3.5f, Normalized(ShotAccuracy));
    public float PassScatterDegrees => Mathf.Lerp(11f, 2.5f, Normalized(PassAccuracy));
    public float TackleReach => Mathf.Lerp(18f, 38f, Normalized(TackleStrength));
    public float BallRetention => Mathf.Lerp(0.7f, 1.4f, Normalized(BallControl));
    public float RecoverySeconds => Mathf.Lerp(0.75f, 0.22f, Normalized(Recovery));
    public float SlideSpeed => Mathf.Lerp(430f, 660f, Normalized(TackleStrength));

    public float BodyRadius =>
        Role switch
        {
            PlayerRole.Big => 25f,
            PlayerRole.Medium => 20f,
            _ => 16f
        };

    public float DribbleDistance =>
        Role switch
        {
            PlayerRole.Big => 18f,
            PlayerRole.Medium => 14f,
            _ => 10f
        };

    private static float Normalized(int value) => Mathf.Clamp((value - 1f) / 4f, 0f, 1f);
}

public sealed class TeamDefinition
{
    public string Name { get; set; }
    public Color PrimaryColor { get; set; }
    public Color AccentColor { get; set; }
    public PlayerRole[] Roles { get; set; }

    public TeamDefinition(string name, Color primaryColor, Color accentColor, PlayerRole[] roles)
    {
        Name = name;
        PrimaryColor = primaryColor;
        AccentColor = accentColor;
        Roles = roles;
    }

    public TeamDefinition Clone()
    {
        return new TeamDefinition(Name, PrimaryColor, AccentColor, (PlayerRole[])Roles.Clone());
    }
}

public sealed class MatchSummary
{
    public int HomeScore { get; }
    public int AwayScore { get; }

    public MatchSummary(int homeScore, int awayScore)
    {
        HomeScore = homeScore;
        AwayScore = awayScore;
    }
}

public static class PocketPitchConfig
{
    public const float FieldWidth = 1380f;
    public const float FieldHeight = 1680f;
    public const float GoalWidth = 220f;
    public const float GoalDepth = 60f;
    public const float PenaltyBoxWidth = 520f;
    public const float PenaltyBoxDepth = 220f;
    public const float MatchLengthSeconds = 120f;
    public const float GoalFreezeSeconds = 0.9f;
    public const float KickoffDelaySeconds = 0.8f;
    public const float BallRadius = 9f;
    public const float BallFriction = 260f;
    public const float WallBounce = 0.78f;
    public const float MaxShotChargeSeconds = 1.2f;

    public static readonly Vector2[] HomeAnchors =
    {
        new(-430f, 520f),
        new(-150f, 330f),
        new(150f, 330f),
        new(430f, 520f),
        new(0f, 90f)
    };

    public static readonly Vector2[] AwayAnchors =
    {
        new(-430f, -520f),
        new(-150f, -330f),
        new(150f, -330f),
        new(430f, -520f),
        new(0f, -90f)
    };

    public static readonly string[] SquadSlots =
    {
        "Left Wing",
        "Left Mid",
        "Right Mid",
        "Right Wing",
        "Striker"
    };

    public static readonly TeamDefinition DefaultHomeTeam = new(
        "Harbor Hearts",
        new Color("2f7cf6"),
        new Color("d8efff"),
        new[]
        {
            PlayerRole.Small,
            PlayerRole.Medium,
            PlayerRole.Big,
            PlayerRole.Medium,
            PlayerRole.Small
        });

    public static readonly TeamDefinition[] AwayPresets =
    {
        new(
            "Sunset City",
            new Color("ff7a18"),
            new Color("fff0da"),
            new[]
            {
                PlayerRole.Big,
                PlayerRole.Medium,
                PlayerRole.Medium,
                PlayerRole.Small,
                PlayerRole.Small
            }),
        new(
            "Evergreen Athletic",
            new Color("25a76d"),
            new Color("e8fff3"),
            new[]
            {
                PlayerRole.Medium,
                PlayerRole.Small,
                PlayerRole.Medium,
                PlayerRole.Small,
                PlayerRole.Big
            }),
        new(
            "Metro Meteors",
            new Color("d9465f"),
            new Color("ffe4ea"),
            new[]
            {
                PlayerRole.Big,
                PlayerRole.Big,
                PlayerRole.Medium,
                PlayerRole.Medium,
                PlayerRole.Small
            })
    };

    public static StatBlock CreateStats(PlayerRole role)
    {
        return role switch
        {
            PlayerRole.Big => new StatBlock(role, 2, 2, 5, 3, 3, 5, 3, 2),
            PlayerRole.Medium => new StatBlock(role, 3, 3, 3, 3, 3, 3, 3, 3),
            _ => new StatBlock(role, 5, 5, 2, 3, 4, 2, 5, 4)
        };
    }

    public static string RoleName(PlayerRole role)
    {
        return role switch
        {
            PlayerRole.Big => "Big",
            PlayerRole.Medium => "Medium",
            _ => "Small"
        };
    }

    public static PlayerRole NextRole(PlayerRole current)
    {
        return current switch
        {
            PlayerRole.Big => PlayerRole.Medium,
            PlayerRole.Medium => PlayerRole.Small,
            _ => PlayerRole.Big
        };
    }

    public static Vector2 GoalCenter(TeamSide defendingSide)
    {
        return defendingSide == TeamSide.Home
            ? new Vector2(0f, FieldHeight * 0.5f)
            : new Vector2(0f, -FieldHeight * 0.5f);
    }

    public static Vector2 AttackingGoalCenter(TeamSide attackingSide)
    {
        return attackingSide == TeamSide.Home ? GoalCenter(TeamSide.Away) : GoalCenter(TeamSide.Home);
    }

    public static Rect2 FieldRect => new(
        new Vector2(-FieldWidth * 0.5f, -FieldHeight * 0.5f),
        new Vector2(FieldWidth, FieldHeight));

    public static Rect2 GoalkeeperRoamRect(TeamSide side)
    {
        var top = side == TeamSide.Away ? -FieldHeight * 0.5f + 18f : FieldHeight * 0.5f - PenaltyBoxDepth - 18f;
        var height = PenaltyBoxDepth - 18f;
        return new Rect2(-PenaltyBoxWidth * 0.5f, top, PenaltyBoxWidth, height);
    }

    public static Rect2 PenaltyAreaRect(TeamSide side)
    {
        var field = FieldRect;
        var top = side == TeamSide.Away ? field.Position.Y : field.End.Y - PenaltyBoxDepth;
        return new Rect2(-PenaltyBoxWidth * 0.5f, top, PenaltyBoxWidth, PenaltyBoxDepth);
    }

    public static IReadOnlyList<TeamDefinition> CreateAwayPresets()
    {
        var clones = new List<TeamDefinition>();
        foreach (var preset in AwayPresets)
        {
            clones.Add(preset.Clone());
        }

        return clones;
    }
}
