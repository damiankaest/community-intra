namespace CommunityIntranet.Modules.CounterStrike.Services;

public sealed class CounterStrikeOptions
{
    public const string SectionName = "CounterStrike";

    public string StorageRoot { get; set; } = "data/counter-strike";
    public int MaximumDemoMegabytes { get; set; } = 512;
    public int ParserTimeoutSeconds { get; set; } = 180;
    public string AnalyzerExecutable { get; set; } = "csda";
    public int QueueCapacity { get; set; } = 32;
    public bool SeedDemoData { get; set; }
}
