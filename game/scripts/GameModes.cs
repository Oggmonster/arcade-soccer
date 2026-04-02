using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public enum MatchControlMode
{
    PlayerVsCpu,
    PlayerVsPlayer
}

public enum WorldCupStage
{
    GroupStage,
    RoundOf16,
    Quarterfinal,
    Semifinal,
    Final,
    Complete
}

public sealed class MatchSettings
{
    public TeamDefinition HomeTeam { get; }
    public TeamDefinition AwayTeam { get; }
    public bool HomeHumanControlled { get; }
    public bool AwayHumanControlled { get; }
    public string PrimaryActionText { get; }
    public string SecondaryActionText { get; }
    public string HeaderText { get; }

    public MatchSettings(
        TeamDefinition homeTeam,
        TeamDefinition awayTeam,
        bool homeHumanControlled,
        bool awayHumanControlled,
        string primaryActionText = "Rematch",
        string secondaryActionText = "Return to Menu",
        string headerText = "")
    {
        HomeTeam = homeTeam.Clone();
        AwayTeam = awayTeam.Clone();
        HomeHumanControlled = homeHumanControlled;
        AwayHumanControlled = awayHumanControlled;
        PrimaryActionText = primaryActionText;
        SecondaryActionText = secondaryActionText;
        HeaderText = headerText;
    }
}

public sealed class MatchResult
{
    public TeamDefinition HomeTeam { get; }
    public TeamDefinition AwayTeam { get; }
    public int HomeScore { get; }
    public int AwayScore { get; }

    public MatchResult(TeamDefinition homeTeam, TeamDefinition awayTeam, int homeScore, int awayScore)
    {
        HomeTeam = homeTeam.Clone();
        AwayTeam = awayTeam.Clone();
        HomeScore = homeScore;
        AwayScore = awayScore;
    }

    public bool IsDraw => HomeScore == AwayScore;
    public string WinnerName => HomeScore > AwayScore ? HomeTeam.Name : AwayScore > HomeScore ? AwayTeam.Name : "Draw";
}

public sealed class NationalTeamProfile
{
    public string Code { get; }
    public TeamDefinition Team { get; }
    public string[] PlayerNames { get; }

    public NationalTeamProfile(string code, TeamDefinition team, string[] playerNames)
    {
        Code = code;
        Team = team;
        PlayerNames = playerNames;
    }

    public NationalTeamProfile Clone()
    {
        return new NationalTeamProfile(Code, Team.Clone(), (string[])PlayerNames.Clone());
    }
}

public static class NationalTeamDatabase
{
    public static readonly IReadOnlyList<NationalTeamProfile> Teams = BuildTeams();

    public static IReadOnlyList<string[]> WorldCupGroups => new[]
    {
        new[] { "Brazil", "Sweden", "Mexico", "Japan" },
        new[] { "France", "Norway", "USA", "Morocco" },
        new[] { "Italy", "Argentina", "Cameroon", "Switzerland" },
        new[] { "Germany", "Colombia", "South Korea", "Australia" },
        new[] { "Spain", "Uruguay", "Canada", "Poland" },
        new[] { "Netherlands", "Denmark", "Senegal", "Austria" },
        new[] { "England", "Portugal", "Nigeria", "Chile" },
        new[] { "Belgium", "Croatia", "Turkey", "Czech Republic" }
    };

    public static NationalTeamProfile GetByName(string teamName)
    {
        return Teams.First(team => team.Team.Name == teamName).Clone();
    }

    private static IReadOnlyList<NationalTeamProfile> BuildTeams()
    {
        return new List<NationalTeamProfile>
        {
            Team("BRA", "Brazil", "f7c948", "0d5d56", Roles(PlayerRole.Small, PlayerRole.Medium, PlayerRole.Big, PlayerRole.Small, PlayerRole.Medium), "Rafa Silva", "Tiago Nunes", "Mateo Costa", "Joao Varela", "Luis Dantas"),
            Team("ITA", "Italy", "3273dc", "edf6ff", Roles(PlayerRole.Medium, PlayerRole.Big, PlayerRole.Medium, PlayerRole.Small, PlayerRole.Medium), "Luca Bellini", "Marco Serra", "Paolo Riva", "Dario Conti", "Elio Vitale"),
            Team("SWE", "Sweden", "f5d547", "2d5fb7", Roles(PlayerRole.Big, PlayerRole.Medium, PlayerRole.Small, PlayerRole.Medium, PlayerRole.Small), "Viktor Lund", "Joel Ekstrom", "Anton Nyberg", "Mikael Sjolin", "Felix Soder"),
            Team("FRA", "France", "2850b8", "f4f6ff", Roles(PlayerRole.Small, PlayerRole.Medium, PlayerRole.Medium, PlayerRole.Big, PlayerRole.Small), "Theo Laurent", "Bastien Renaud", "Nolan Mercier", "Hugo Fabre", "Ilyes Marin"),
            Team("ESP", "Spain", "d93d3d", "ffd8a8", Roles(PlayerRole.Small, PlayerRole.Small, PlayerRole.Medium, PlayerRole.Medium, PlayerRole.Big), "Iker Vidal", "Sergio Mena", "Pablo Cano", "Dani Ibanez", "Alvaro Rojo"),
            Team("NED", "Netherlands", "ff7a1a", "fff1df", Roles(PlayerRole.Big, PlayerRole.Medium, PlayerRole.Small, PlayerRole.Medium, PlayerRole.Small), "Jens de Wit", "Milan Smit", "Noah Visser", "Timo van Dijk", "Bram Kuipers"),
            Team("ARG", "Argentina", "70c8ff", "effbff", Roles(PlayerRole.Small, PlayerRole.Medium, PlayerRole.Small, PlayerRole.Big, PlayerRole.Medium), "Tomas Quiroga", "Agustin Ferreyra", "Nico Suarez", "Matias Roldan", "Franco Vega"),
            Team("GER", "Germany", "252525", "f1f1f1", Roles(PlayerRole.Big, PlayerRole.Medium, PlayerRole.Medium, PlayerRole.Small, PlayerRole.Medium), "Leon Hartmann", "Niklas Vogt", "Jonas Kruger", "Felix Brandt", "Timo Adler"),
            Team("NOR", "Norway", "d93535", "f6fbff", Roles(PlayerRole.Big, PlayerRole.Small, PlayerRole.Medium, PlayerRole.Medium, PlayerRole.Small), "Elias Strand", "Sander Moen", "Jonas Bjerke", "Kasper Hauge", "Lars Aasen"),
            Team("ENG", "England", "ffffff", "d23a3a", Roles(PlayerRole.Medium, PlayerRole.Big, PlayerRole.Small, PlayerRole.Medium, PlayerRole.Small), "Owen Mercer", "Callum Price", "Jack Harlow", "Ben Carter", "Liam Foster"),
            Team("POR", "Portugal", "118a58", "f5e7e0", Roles(PlayerRole.Small, PlayerRole.Medium, PlayerRole.Small, PlayerRole.Big, PlayerRole.Medium), "Rui Correia", "Diogo Mendes", "Nuno Pires", "Goncalo Freitas", "Andre Mota"),
            Team("DEN", "Denmark", "c93030", "fff5f5", Roles(PlayerRole.Medium, PlayerRole.Small, PlayerRole.Big, PlayerRole.Medium, PlayerRole.Small), "Mads Holm", "Jonas Friis", "Viggo Krogh", "Emil Boesen", "Oliver Skov"),
            Team("CRO", "Croatia", "e24b4b", "f4fbff", Roles(PlayerRole.Medium, PlayerRole.Big, PlayerRole.Medium, PlayerRole.Small, PlayerRole.Small), "Luka Marinic", "Ante Kovac", "Ivan Bender", "Niko Lucic", "Petar Zoric"),
            Team("BEL", "Belgium", "b52222", "ffe08a", Roles(PlayerRole.Medium, PlayerRole.Small, PlayerRole.Medium, PlayerRole.Big, PlayerRole.Small), "Arthur Vanden", "Milo Janssens", "Noa Maes", "Yanis Claes", "Julien Deryck"),
            Team("URU", "Uruguay", "7bc8ff", "f8fbff", Roles(PlayerRole.Big, PlayerRole.Medium, PlayerRole.Small, PlayerRole.Medium, PlayerRole.Small), "Santiago Pereyra", "Bruno Cabrera", "Facundo Luz", "Leandro Sena", "Diego Olivera"),
            Team("MEX", "Mexico", "12905d", "fff0e3", Roles(PlayerRole.Small, PlayerRole.Medium, PlayerRole.Small, PlayerRole.Medium, PlayerRole.Big), "Mateo Cardenas", "Julian Nava", "Gael Pineda", "Emilio Solis", "Axel Campos"),
            Team("USA", "USA", "1f4aa8", "eef3ff", Roles(PlayerRole.Big, PlayerRole.Medium, PlayerRole.Small, PlayerRole.Medium, PlayerRole.Small), "Logan Pierce", "Mason Hayes", "Evan Brooks", "Noah Turner", "Cole Bennett"),
            Team("JPN", "Japan", "2f53c5", "fff7f7", Roles(PlayerRole.Small, PlayerRole.Small, PlayerRole.Medium, PlayerRole.Medium, PlayerRole.Big), "Haru Sato", "Ren Takahashi", "Yuto Nishida", "Daiki Okada", "Soma Fujita"),
            Team("KOR", "South Korea", "d94242", "f4fbff", Roles(PlayerRole.Small, PlayerRole.Medium, PlayerRole.Small, PlayerRole.Big, PlayerRole.Medium), "Min-jun Park", "Seo-jun Kim", "Ji-ho Lee", "Hyun-woo Choi", "Tae-yang Han"),
            Team("MAR", "Morocco", "bf2f38", "1e8a64", Roles(PlayerRole.Medium, PlayerRole.Small, PlayerRole.Medium, PlayerRole.Big, PlayerRole.Small), "Youssef Amrani", "Rayan Idrissi", "Hamza Sabiri", "Nabil El Fassi", "Karim Bakkali"),
            Team("CMR", "Cameroon", "2f9e44", "f8d95f", Roles(PlayerRole.Big, PlayerRole.Medium, PlayerRole.Small, PlayerRole.Medium, PlayerRole.Small), "Joel Ndzi", "Kylian Mbarga", "Boris Etame", "Cedric Ndzi", "Arnaud Tchoua"),
            Team("SEN", "Senegal", "1a9c68", "f5f0c8", Roles(PlayerRole.Big, PlayerRole.Small, PlayerRole.Medium, PlayerRole.Small, PlayerRole.Medium), "Mamadou Seck", "Ibrahima Faye", "Cheikh Ndiaye", "Alioune Diallo", "Saliou Ba"),
            Team("NGA", "Nigeria", "1f9d55", "e9fff4", Roles(PlayerRole.Big, PlayerRole.Small, PlayerRole.Medium, PlayerRole.Medium, PlayerRole.Small), "Tunde Balogun", "Femi Okoye", "Seyi Afolabi", "Kelechi Nwosu", "David Eze"),
            Team("POL", "Poland", "ffffff", "d93838", Roles(PlayerRole.Big, PlayerRole.Medium, PlayerRole.Medium, PlayerRole.Small, PlayerRole.Small), "Jan Zielinski", "Marek Kowal", "Piotr Nowak", "Filip Dabrowski", "Oskar Lis"),
            Team("SUI", "Switzerland", "d53a3a", "f9f9f9", Roles(PlayerRole.Medium, PlayerRole.Big, PlayerRole.Small, PlayerRole.Medium, PlayerRole.Small), "Luca Meier", "Noel Baumann", "Yann Vogel", "Milo Schmid", "Timo Keller"),
            Team("AUT", "Austria", "d43b3b", "fff7f7", Roles(PlayerRole.Medium, PlayerRole.Medium, PlayerRole.Small, PlayerRole.Big, PlayerRole.Small), "Matteo Gruber", "Felix Hofer", "Jonas Leitner", "David Eder", "Paul Huber"),
            Team("CZE", "Czech Republic", "d73d3d", "f3f7ff", Roles(PlayerRole.Medium, PlayerRole.Big, PlayerRole.Small, PlayerRole.Medium, PlayerRole.Small), "Adam Novak", "Tomas Krejci", "Jakub Vesely", "Daniel Sykora", "Filip Maly"),
            Team("COL", "Colombia", "f8d441", "1837a6", Roles(PlayerRole.Small, PlayerRole.Medium, PlayerRole.Small, PlayerRole.Big, PlayerRole.Medium), "Juan Ocampo", "Mateo Delgado", "Andres Pardo", "Nicolas Rios", "Samuel Cuesta"),
            Team("CHI", "Chile", "d62f2f", "f5f9ff", Roles(PlayerRole.Small, PlayerRole.Medium, PlayerRole.Big, PlayerRole.Medium, PlayerRole.Small), "Diego Araya", "Cristobal Mena", "Vicente Salas", "Benjamin Toro", "Joaquin Farfan"),
            Team("TUR", "Turkey", "d53030", "fff6f6", Roles(PlayerRole.Big, PlayerRole.Medium, PlayerRole.Small, PlayerRole.Medium, PlayerRole.Small), "Arda Demir", "Kerem Yildiz", "Ege Aydin", "Mert Aksoy", "Can Kaya"),
            Team("AUS", "Australia", "f0c13a", "0d6a4f", Roles(PlayerRole.Big, PlayerRole.Medium, PlayerRole.Small, PlayerRole.Medium, PlayerRole.Small), "Lachlan Reed", "Mason Kerr", "Ethan Doyle", "Hudson Blake", "Noah Riley"),
            Team("CAN", "Canada", "d53333", "f5f7ff", Roles(PlayerRole.Big, PlayerRole.Small, PlayerRole.Medium, PlayerRole.Small, PlayerRole.Medium), "Owen Sinclair", "Lucas McLeod", "Eli Mercer", "Wyatt Benoit", "Nolan Fraser")
        };
    }

    private static NationalTeamProfile Team(
        string code,
        string name,
        string primaryColor,
        string accentColor,
        PlayerRole[] roles,
        params string[] playerNames)
    {
        var expandedRoles = ExpandRoles(roles);
        return new NationalTeamProfile(
            code,
            new TeamDefinition(name, new Color(primaryColor), new Color(accentColor), expandedRoles),
            ExpandPlayerNames(playerNames, expandedRoles.Length));
    }

    private static PlayerRole[] Roles(
        PlayerRole leftWing,
        PlayerRole leftMid,
        PlayerRole rightMid,
        PlayerRole rightWing,
        PlayerRole striker)
    {
        return new[] { leftWing, leftMid, rightMid, rightWing, striker };
    }

    private static PlayerRole[] ExpandRoles(PlayerRole[] baseRoles)
    {
        if (baseRoles.Length >= PocketPitchConfig.OutfieldPlayerCount)
        {
            return baseRoles.ToArray();
        }

        var leftWing = baseRoles[0];
        var leftMid = baseRoles[1];
        var rightMid = baseRoles[2];
        var rightWing = baseRoles[3];
        var striker = baseRoles[4];

        return new[]
        {
            leftWing,
            leftMid,
            ChooseCenterMid(leftMid, rightMid),
            rightMid,
            rightWing,
            ChooseSupportStriker(striker, leftWing, rightWing),
            striker
        };
    }

    private static PlayerRole ChooseCenterMid(PlayerRole leftMid, PlayerRole rightMid)
    {
        if (leftMid == rightMid)
        {
            return leftMid;
        }

        if (leftMid == PlayerRole.Big || rightMid == PlayerRole.Big)
        {
            return PlayerRole.Big;
        }

        if (leftMid == PlayerRole.Small && rightMid == PlayerRole.Small)
        {
            return PlayerRole.Small;
        }

        return PlayerRole.Medium;
    }

    private static PlayerRole ChooseSupportStriker(PlayerRole striker, PlayerRole leftWing, PlayerRole rightWing)
    {
        if (striker == PlayerRole.Big)
        {
            return PlayerRole.Medium;
        }

        if (striker == PlayerRole.Small)
        {
            return leftWing == PlayerRole.Small || rightWing == PlayerRole.Small
                ? PlayerRole.Small
                : PlayerRole.Medium;
        }

        return leftWing == PlayerRole.Big || rightWing == PlayerRole.Big
            ? PlayerRole.Big
            : PlayerRole.Medium;
    }

    private static string[] ExpandPlayerNames(string[] baseNames, int requiredCount)
    {
        if (baseNames.Length >= requiredCount)
        {
            return baseNames.ToArray();
        }

        var names = new List<string>(baseNames);
        var parts = baseNames
            .Select(name =>
            {
                var split = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return new
                {
                    First = split.FirstOrDefault() ?? "Alex",
                    Last = split.Length > 1 ? split[^1] : "Vale"
                };
            })
            .ToArray();

        for (var offset = 1; names.Count < requiredCount && offset < parts.Length + 2; offset++)
        {
            for (var i = 0; i < parts.Length && names.Count < requiredCount; i++)
            {
                var first = parts[i].First;
                var last = parts[(i + offset) % parts.Length].Last;
                var candidate = $"{first} {last}";
                if (!names.Contains(candidate, StringComparer.Ordinal))
                {
                    names.Add(candidate);
                }
            }
        }

        while (names.Count < requiredCount)
        {
            names.Add($"Player {names.Count + 1}");
        }

        return names.ToArray();
    }
}

public sealed class WorldCupStandingRow
{
    public string TeamName { get; }
    public int Played { get; private set; }
    public int Wins { get; private set; }
    public int Draws { get; private set; }
    public int Losses { get; private set; }
    public int GoalsFor { get; private set; }
    public int GoalsAgainst { get; private set; }
    public int Points { get; private set; }

    public WorldCupStandingRow(string teamName)
    {
        TeamName = teamName;
    }

    public int GoalDifference => GoalsFor - GoalsAgainst;

    public void Register(int goalsFor, int goalsAgainst)
    {
        Played++;
        GoalsFor += goalsFor;
        GoalsAgainst += goalsAgainst;

        if (goalsFor > goalsAgainst)
        {
            Wins++;
            Points += 3;
        }
        else if (goalsFor == goalsAgainst)
        {
            Draws++;
            Points += 1;
        }
        else
        {
            Losses++;
        }
    }
}

public sealed class WorldCupFixture
{
    public string TeamA { get; }
    public string TeamB { get; }
    public int? GoalsA { get; private set; }
    public int? GoalsB { get; private set; }
    public string StageLabel { get; }
    public string Notes { get; private set; } = string.Empty;

    public WorldCupFixture(string teamA, string teamB, string stageLabel)
    {
        TeamA = teamA;
        TeamB = teamB;
        StageLabel = stageLabel;
    }

    public bool IsPlayed => GoalsA.HasValue && GoalsB.HasValue;

    public void SetScore(int goalsA, int goalsB, string notes = "")
    {
        GoalsA = goalsA;
        GoalsB = goalsB;
        Notes = notes;
    }

    public string Winner => !IsPlayed
        ? string.Empty
        : GoalsA!.Value > GoalsB!.Value ? TeamA : GoalsB.Value > GoalsA.Value ? TeamB : "Draw";
}

public sealed class WorldCupTournament
{
    private readonly RandomNumberGenerator _rng = new();
    private readonly Dictionary<string, NationalTeamProfile> _profilesByName;
    private readonly Dictionary<string, WorldCupStandingRow> _standings;
    private readonly List<string[]> _groups;
    private readonly List<WorldCupFixture[]> _groupRounds = new();
    private readonly List<WorldCupFixture> _roundOf16 = new();
    private readonly List<WorldCupFixture> _quarterfinals = new();
    private readonly List<WorldCupFixture> _semifinals = new();
    private WorldCupFixture? _final;
    private WorldCupFixture? _currentPlayableFixture;
    private int _currentGroupRound;

    public NationalTeamProfile PlayerTeam { get; }
    public WorldCupStage Stage { get; private set; } = WorldCupStage.GroupStage;
    public string StatusText { get; private set; } = "World Cup ready.";
    public string ChampionName { get; private set; } = string.Empty;
    public bool PlayerEliminated { get; private set; }

    public WorldCupTournament(string playerTeamName)
    {
        _rng.Randomize();
        _profilesByName = NationalTeamDatabase.Teams.ToDictionary(profile => profile.Team.Name, profile => profile.Clone());
        _groups = NationalTeamDatabase.WorldCupGroups.Select(group => group.ToArray()).ToList();
        _standings = _profilesByName.Keys.ToDictionary(name => name, name => new WorldCupStandingRow(name));
        PlayerTeam = _profilesByName[playerTeamName];
        BuildGroupRounds();
        PrepareCurrentGroupRound();
    }

    public string CurrentStageLabel =>
        Stage switch
        {
            WorldCupStage.GroupStage => $"Group Stage - Matchday {_currentGroupRound + 1}",
            WorldCupStage.RoundOf16 => "Round of 16",
            WorldCupStage.Quarterfinal => "Quarterfinal",
            WorldCupStage.Semifinal => "Semifinal",
            WorldCupStage.Final => "Final",
            _ => ChampionName == PlayerTeam.Team.Name ? "You won the World Cup!" : $"Champion: {ChampionName}"
        };

    public bool HasPlayableMatch => _currentPlayableFixture != null;

    public MatchSettings CreateNextMatchSettings()
    {
        if (_currentPlayableFixture == null)
        {
            throw new InvalidOperationException("No playable match is currently available.");
        }
        var homeProfile = _profilesByName[_currentPlayableFixture.TeamA];
        var awayProfile = _profilesByName[_currentPlayableFixture.TeamB];
        var playerIsHome = _currentPlayableFixture.TeamA == PlayerTeam.Team.Name;

        return new MatchSettings(
            homeProfile.Team,
            awayProfile.Team,
            playerIsHome,
            !playerIsHome,
            "Continue Tournament",
            "Return to Menu",
            _currentPlayableFixture.StageLabel);
    }

    public void ApplyPlayedMatch(MatchResult result)
    {
        if (_currentPlayableFixture == null)
        {
            return;
        }

        var fixture = _currentPlayableFixture;
        var resultMapsDirectly = result.HomeTeam.Name == fixture.TeamA && result.AwayTeam.Name == fixture.TeamB;
        var goalsA = resultMapsDirectly ? result.HomeScore : result.AwayScore;
        var goalsB = resultMapsDirectly ? result.AwayScore : result.HomeScore;

        if (Stage == WorldCupStage.GroupStage || !result.IsDraw)
        {
            fixture.SetScore(goalsA, goalsB);
        }
        else
        {
            var playerEdge = TeamAttackRating(PlayerTeam.Team) + _rng.Randf();
            var opponentEdge = TeamAttackRating(_profilesByName[OpponentFor(fixture)].Team) + _rng.Randf();
            var playerIsTeamA = fixture.TeamA == PlayerTeam.Team.Name;
            if (playerEdge >= opponentEdge)
            {
                fixture.SetScore(
                    playerIsTeamA ? goalsA + 1 : goalsA,
                    playerIsTeamA ? goalsB : goalsB + 1,
                    "Won after penalties");
            }
            else
            {
                fixture.SetScore(
                    playerIsTeamA ? goalsA : goalsA + 1,
                    playerIsTeamA ? goalsB + 1 : goalsB,
                    "Lost after penalties");
            }
        }

        if (Stage == WorldCupStage.GroupStage)
        {
            RegisterStandings(fixture.TeamA, fixture.GoalsA!.Value, fixture.GoalsB!.Value, fixture);
        }

        if (Stage == WorldCupStage.GroupStage)
        {
            _currentGroupRound++;
            if (_currentGroupRound < _groupRounds.Count)
            {
                PrepareCurrentGroupRound();
                return;
            }

            AdvanceFromGroups();
            return;
        }

        AdvanceKnockoutStage();
    }

    public string BuildGroupTableText()
    {
        var lines = new List<string>();
        for (var groupIndex = 0; groupIndex < _groups.Count; groupIndex++)
        {
            var groupName = $"Group {(char)('A' + groupIndex)}";
            lines.Add(groupName);
            var rows = _groups[groupIndex]
                .Select(name => _standings[name])
                .OrderByDescending(row => row.Points)
                .ThenByDescending(row => row.GoalDifference)
                .ThenByDescending(row => row.GoalsFor)
                .ThenBy(row => row.TeamName)
                .ToList();

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                lines.Add($"{i + 1}. {row.TeamName}  {row.Points} pts  {row.GoalsFor}:{row.GoalsAgainst}");
            }

            lines.Add(string.Empty);
        }

        return string.Join('\n', lines).Trim();
    }

    public string BuildBracketText()
    {
        var lines = new List<string>();
        AddFixtureLines(lines, "Round of 16", _roundOf16);
        AddFixtureLines(lines, "Quarterfinals", _quarterfinals);
        AddFixtureLines(lines, "Semifinals", _semifinals);
        AddFixtureLines(lines, "Final", _final == null ? Array.Empty<WorldCupFixture>() : new[] { _final });
        return string.Join('\n', lines).Trim();
    }

    private void BuildGroupRounds()
    {
        for (var round = 0; round < 3; round++)
        {
            _groupRounds.Add(new WorldCupFixture[_groups.Count * 2]);
        }

        for (var groupIndex = 0; groupIndex < _groups.Count; groupIndex++)
        {
            var teams = _groups[groupIndex];
            var schedule = new[]
            {
                new[] { (teams[0], teams[1]), (teams[2], teams[3]) },
                new[] { (teams[0], teams[2]), (teams[1], teams[3]) },
                new[] { (teams[0], teams[3]), (teams[1], teams[2]) }
            };

            for (var round = 0; round < schedule.Length; round++)
            {
                var offset = groupIndex * 2;
                _groupRounds[round][offset] = new WorldCupFixture(schedule[round][0].Item1, schedule[round][0].Item2, $"Group {(char)('A' + groupIndex)}");
                _groupRounds[round][offset + 1] = new WorldCupFixture(schedule[round][1].Item1, schedule[round][1].Item2, $"Group {(char)('A' + groupIndex)}");
            }
        }
    }

    private void PrepareCurrentGroupRound()
    {
        _currentPlayableFixture = null;
        foreach (var fixture in _groupRounds[_currentGroupRound])
        {
            if (fixture.TeamA == PlayerTeam.Team.Name || fixture.TeamB == PlayerTeam.Team.Name)
            {
                _currentPlayableFixture = fixture;
                continue;
            }

            if (!fixture.IsPlayed)
            {
                SimulateFixture(fixture, allowDraw: true);
            }
        }

        StatusText = _currentPlayableFixture == null
            ? "No player fixture found."
            : $"Next up: {PlayerTeam.Team.Name} vs {OpponentFor(_currentPlayableFixture)}";
    }

    private void AdvanceFromGroups()
    {
        var qualifiers = new List<string>();
        foreach (var group in _groups)
        {
            var topTwo = group
                .Select(name => _standings[name])
                .OrderByDescending(row => row.Points)
                .ThenByDescending(row => row.GoalDifference)
                .ThenByDescending(row => row.GoalsFor)
                .ThenBy(row => row.TeamName)
                .Take(2)
                .Select(row => row.TeamName)
                .ToArray();
            qualifiers.Add(topTwo[0]);
            qualifiers.Add(topTwo[1]);
        }

        if (!qualifiers.Contains(PlayerTeam.Team.Name))
        {
            PlayerEliminated = true;
            StatusText = $"{PlayerTeam.Team.Name} were knocked out in the groups.";
            BuildKnockoutFixtures(qualifiers);
            SimulateTournamentToCompletion();
            Stage = WorldCupStage.Complete;
            return;
        }

        BuildKnockoutFixtures(qualifiers);
        Stage = WorldCupStage.RoundOf16;
        PrepareKnockoutStage(_roundOf16, WorldCupStage.RoundOf16);
    }

    private void BuildKnockoutFixtures(List<string> qualifiers)
    {
        _roundOf16.Clear();
        _quarterfinals.Clear();
        _semifinals.Clear();
        _final = null;

        _roundOf16.Add(new WorldCupFixture(qualifiers[0], qualifiers[3], "Round of 16"));
        _roundOf16.Add(new WorldCupFixture(qualifiers[4], qualifiers[7], "Round of 16"));
        _roundOf16.Add(new WorldCupFixture(qualifiers[8], qualifiers[11], "Round of 16"));
        _roundOf16.Add(new WorldCupFixture(qualifiers[12], qualifiers[15], "Round of 16"));
        _roundOf16.Add(new WorldCupFixture(qualifiers[2], qualifiers[1], "Round of 16"));
        _roundOf16.Add(new WorldCupFixture(qualifiers[6], qualifiers[5], "Round of 16"));
        _roundOf16.Add(new WorldCupFixture(qualifiers[10], qualifiers[9], "Round of 16"));
        _roundOf16.Add(new WorldCupFixture(qualifiers[14], qualifiers[13], "Round of 16"));
    }

    private void PrepareKnockoutStage(List<WorldCupFixture> fixtures, WorldCupStage stage)
    {
        Stage = stage;
        _currentPlayableFixture = null;

        foreach (var fixture in fixtures)
        {
            if (fixture.TeamA == PlayerTeam.Team.Name || fixture.TeamB == PlayerTeam.Team.Name)
            {
                _currentPlayableFixture = fixture;
                continue;
            }

            if (!fixture.IsPlayed)
            {
                SimulateFixture(fixture, allowDraw: false);
            }
        }

        StatusText = _currentPlayableFixture == null
            ? $"Simulating {CurrentStageLabel}..."
            : $"{CurrentStageLabel}: {PlayerTeam.Team.Name} vs {OpponentFor(_currentPlayableFixture)}";
    }

    private void AdvanceKnockoutStage()
    {
        if (Stage == WorldCupStage.RoundOf16)
        {
            var playerAdvanced = _currentPlayableFixture!.Winner == PlayerTeam.Team.Name;
            BuildNextKnockoutRound(_roundOf16, _quarterfinals, "Quarterfinal");
            if (!playerAdvanced)
            {
                PlayerEliminated = true;
                StatusText = $"{PlayerTeam.Team.Name} were knocked out in the Round of 16.";
                SimulateTournamentToCompletion();
                Stage = WorldCupStage.Complete;
                return;
            }

            PrepareKnockoutStage(_quarterfinals, WorldCupStage.Quarterfinal);
            return;
        }

        if (Stage == WorldCupStage.Quarterfinal)
        {
            var playerAdvanced = _currentPlayableFixture!.Winner == PlayerTeam.Team.Name;
            BuildNextKnockoutRound(_quarterfinals, _semifinals, "Semifinal");
            if (!playerAdvanced)
            {
                PlayerEliminated = true;
                StatusText = $"{PlayerTeam.Team.Name} fell in the quarterfinals.";
                SimulateTournamentToCompletion();
                Stage = WorldCupStage.Complete;
                return;
            }

            PrepareKnockoutStage(_semifinals, WorldCupStage.Semifinal);
            return;
        }

        if (Stage == WorldCupStage.Semifinal)
        {
            var playerAdvanced = _currentPlayableFixture!.Winner == PlayerTeam.Team.Name;
            _final = new WorldCupFixture(_semifinals[0].Winner, _semifinals[1].Winner, "Final");
            if (!playerAdvanced)
            {
                PlayerEliminated = true;
                SimulateFixture(_final, allowDraw: false);
                ChampionName = _final.Winner;
                StatusText = $"{PlayerTeam.Team.Name} lost in the semifinal. {ChampionName} won the cup.";
                Stage = WorldCupStage.Complete;
                return;
            }

            PrepareKnockoutStage(new List<WorldCupFixture> { _final }, WorldCupStage.Final);
            return;
        }

        if (Stage == WorldCupStage.Final)
        {
            ChampionName = _currentPlayableFixture!.Winner;
            Stage = WorldCupStage.Complete;
            StatusText = ChampionName == PlayerTeam.Team.Name
                ? $"{PlayerTeam.Team.Name} are world champions!"
                : $"{PlayerTeam.Team.Name} lost the final. {ChampionName} lifted the trophy.";
        }
    }

    private void BuildNextKnockoutRound(List<WorldCupFixture> previousRound, List<WorldCupFixture> nextRound, string stageLabel)
    {
        nextRound.Clear();
        for (var i = 0; i < previousRound.Count; i += 2)
        {
            nextRound.Add(new WorldCupFixture(previousRound[i].Winner, previousRound[i + 1].Winner, stageLabel));
        }
    }

    private void SimulateTournamentToCompletion()
    {
        if (_roundOf16.Any(fixture => !fixture.IsPlayed))
        {
            foreach (var fixture in _roundOf16.Where(fixture => !fixture.IsPlayed))
            {
                SimulateFixture(fixture, allowDraw: false);
            }
        }

        if (_quarterfinals.Count == 0)
        {
            BuildNextKnockoutRound(_roundOf16, _quarterfinals, "Quarterfinal");
        }

        foreach (var fixture in _quarterfinals.Where(fixture => !fixture.IsPlayed))
        {
            SimulateFixture(fixture, allowDraw: false);
        }

        if (_semifinals.Count == 0)
        {
            BuildNextKnockoutRound(_quarterfinals, _semifinals, "Semifinal");
        }

        foreach (var fixture in _semifinals.Where(fixture => !fixture.IsPlayed))
        {
            SimulateFixture(fixture, allowDraw: false);
        }

        if (_final == null)
        {
            _final = new WorldCupFixture(_semifinals[0].Winner, _semifinals[1].Winner, "Final");
        }

        if (!_final.IsPlayed)
        {
            SimulateFixture(_final, allowDraw: false);
        }

        ChampionName = _final.Winner;
    }

    private void SimulateFixture(WorldCupFixture fixture, bool allowDraw)
    {
        var teamA = _profilesByName[fixture.TeamA];
        var teamB = _profilesByName[fixture.TeamB];
        var attackA = TeamAttackRating(teamA.Team);
        var attackB = TeamAttackRating(teamB.Team);
        var defenseA = TeamDefenseRating(teamA.Team);
        var defenseB = TeamDefenseRating(teamB.Team);

        var goalsA = SimulatedGoals(attackA, defenseB);
        var goalsB = SimulatedGoals(attackB, defenseA);
        var notes = string.Empty;

        if (!allowDraw && goalsA == goalsB)
        {
            if (_rng.Randf() + (attackA * 0.02f) > _rng.Randf() + (attackB * 0.02f))
            {
                goalsA++;
                notes = "Won after penalties";
            }
            else
            {
                goalsB++;
                notes = "Won after penalties";
            }
        }

        fixture.SetScore(goalsA, goalsB, notes);
        if (fixture.StageLabel.StartsWith("Group", StringComparison.Ordinal))
        {
            RegisterStandings(fixture.TeamA, goalsA, goalsB, fixture);
        }
    }

    private void RegisterStandings(string homeTeamName, int homeGoals, int awayGoals, WorldCupFixture fixture)
    {
        var actualHome = fixture.TeamA == homeTeamName ? fixture.TeamA : fixture.TeamB;
        var actualAway = fixture.TeamA == homeTeamName ? fixture.TeamB : fixture.TeamA;
        var goalsForA = fixture.TeamA == homeTeamName ? homeGoals : awayGoals;
        var goalsForB = fixture.TeamA == homeTeamName ? awayGoals : homeGoals;

        _standings[actualHome].Register(goalsForA, goalsForB);
        _standings[actualAway].Register(goalsForB, goalsForA);
    }

    private float TeamAttackRating(TeamDefinition team)
    {
        var total = 0f;
        foreach (var role in team.Roles)
        {
            var stats = PocketPitchConfig.CreateStats(role);
            total += (stats.ShotPower * 1.35f) + stats.PassAccuracy + (stats.BallControl * 0.8f);
        }

        return total / team.Roles.Length;
    }

    private float TeamDefenseRating(TeamDefinition team)
    {
        var total = 0f;
        foreach (var role in team.Roles)
        {
            var stats = PocketPitchConfig.CreateStats(role);
            total += (stats.TackleStrength * 1.4f) + stats.Recovery + (stats.BallControl * 0.5f);
        }

        return total / team.Roles.Length;
    }

    private int SimulatedGoals(float attack, float defense)
    {
        var chance = Mathf.Clamp((attack - (defense * 0.58f)) * 0.16f + _rng.RandfRange(0.35f, 1.85f), 0.1f, 3.85f);
        return Mathf.Clamp(Mathf.RoundToInt(chance + _rng.RandfRange(-0.6f, 0.8f)), 0, 5);
    }

    private string OpponentFor(WorldCupFixture fixture)
    {
        return fixture.TeamA == PlayerTeam.Team.Name ? fixture.TeamB : fixture.TeamA;
    }

    private static void AddFixtureLines(List<string> lines, string title, IReadOnlyList<WorldCupFixture> fixtures)
    {
        if (fixtures.Count == 0)
        {
            return;
        }

        lines.Add(title);
        foreach (var fixture in fixtures)
        {
            var scoreText = fixture.IsPlayed
                ? $"{fixture.GoalsA}-{fixture.GoalsB}"
                : "vs";
            var notes = string.IsNullOrWhiteSpace(fixture.Notes) ? string.Empty : $" ({fixture.Notes})";
            lines.Add($"{fixture.TeamA} {scoreText} {fixture.TeamB}{notes}");
        }

        lines.Add(string.Empty);
    }
}
