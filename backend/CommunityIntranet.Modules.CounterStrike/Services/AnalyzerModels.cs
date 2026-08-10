using System.Text.Json.Serialization;

namespace CommunityIntranet.Modules.CounterStrike.Services;

public sealed class AnalyzerMatchDto
{
    public string Checksum { get; set; } = string.Empty;
    public string MapName { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public AnalyzerTeamDto TeamA { get; set; } = new();
    public AnalyzerTeamDto TeamB { get; set; } = new();
    public AnalyzerTeamDto? Winner { get; set; }
    public Dictionary<string, AnalyzerPlayerDto> Players { get; set; } = [];
    public List<AnalyzerRoundDto> Rounds { get; set; } = [];
    public List<AnalyzerKillDto> Kills { get; set; } = [];
    public List<AnalyzerClutchDto> Clutches { get; set; } = [];
    public List<AnalyzerBombDefusedDto> BombsDefused { get; set; } = [];
}

public sealed class AnalyzerTeamDto
{
    public string Name { get; set; } = string.Empty;
    public string Letter { get; set; } = string.Empty;
    public int Score { get; set; }
}

public sealed class AnalyzerPlayerDto
{
    public ulong SteamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public AnalyzerTeamDto Team { get; set; } = new();
    public int KillCount { get; set; }
    public int DeathCount { get; set; }
    public int AssistCount { get; set; }
    public double AverageDamagePerRound { get; set; }
    public double Kast { get; set; }
    public double HeadshotPercent { get; set; }
    public int UtilityDamage { get; set; }
    public int FirstKillCount { get; set; }
    public int FirstDeathCount { get; set; }
    public int TradeKillCount { get; set; }
    public int BombPlantedCount { get; set; }
    public int BombDefusedCount { get; set; }
    public double HltvRating2 { get; set; }
    public int ThreeKillCount { get; set; }
    public int FourKillCount { get; set; }
    public int FiveKillCount { get; set; }
}

public sealed class AnalyzerRoundDto
{
    public int Number { get; set; }
    public int StartTick { get; set; }
    public int EndTick { get; set; }
    public string WinnerName { get; set; } = string.Empty;
    public int TeamAScore { get; set; }
    public int TeamBScore { get; set; }
}

public sealed class AnalyzerKillDto
{
    public int Tick { get; set; }
    public int RoundNumber { get; set; }
    public string WeaponName { get; set; } = string.Empty;
    public string KillerName { get; set; } = string.Empty;
    public ulong KillerSteamId { get; set; }
    public string KillerTeamName { get; set; } = string.Empty;
    public string VictimName { get; set; } = string.Empty;
    public ulong VictimSteamId { get; set; }
    public string VictimTeamName { get; set; } = string.Empty;
    public bool IsHeadshot { get; set; }
    public int PenetratedObjects { get; set; }
    public bool IsThroughSmoke { get; set; }
    public bool IsNoScope { get; set; }
    public bool IsTradeKill { get; set; }
}

public sealed class AnalyzerClutchDto
{
    public int Tick { get; set; }
    public int RoundNumber { get; set; }
    public int OpponentCount { get; set; }
    public bool HasWon { get; set; }
    public ulong ClutcherSteamId { get; set; }
    public string ClutcherName { get; set; } = string.Empty;
    public int ClutcherKillCount { get; set; }
}

public sealed class AnalyzerBombDefusedDto
{
    public int Tick { get; set; }
    public int RoundNumber { get; set; }
    [JsonPropertyName("defuserSteamId")]
    public ulong DefuserSteamId { get; set; }
    public string DefuserName { get; set; } = string.Empty;
}

public sealed record CounterStrikeAnalyzerResult(
    AnalyzerMatchDto Match,
    string ArtifactPath,
    TimeSpan Duration);

public interface ICounterStrikeDemoAnalyzer
{
    Task<CounterStrikeAnalyzerResult> AnalyzeAsync(
        string demoPath,
        string artifactPath,
        CancellationToken cancellationToken);
}
