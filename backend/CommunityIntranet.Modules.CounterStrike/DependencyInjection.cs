using CommunityIntranet.Modules.CounterStrike.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityIntranet.Modules.CounterStrike;

public static class DependencyInjection
{
    public static IServiceCollection AddCounterStrikeModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(CounterStrikeOptions.SectionName);
        var settings = section.Get<CounterStrikeOptions>() ?? new CounterStrikeOptions();
        services.AddOptions<CounterStrikeOptions>()
            .Bind(section)
            .Validate(options => options.MaximumDemoMegabytes is >= 16 and <= 2048,
                "CounterStrike demo size must be between 16 and 2048 MB.")
            .Validate(options => options.ParserTimeoutSeconds is >= 15 and <= 900,
                "CounterStrike parser timeout must be between 15 and 900 seconds.")
            .ValidateOnStart();
        services.Configure<FormOptions>(options =>
            options.MultipartBodyLengthLimit = Math.Max(
                options.MultipartBodyLengthLimit,
                settings.MaximumDemoMegabytes * 1024L * 1024L + 1024L * 1024L));

        services.AddSingleton<ICounterStrikeDemoStorage, CounterStrikeDemoStorage>();
        services.AddSingleton<CounterStrikeDemoQueue>();
        services.AddSingleton<ICounterStrikeDemoQueue>(provider =>
            provider.GetRequiredService<CounterStrikeDemoQueue>());
        services.AddHostedService<CounterStrikeDemoWorker>();
        services.AddScoped<ICounterStrikeDemoAnalyzer, CsdaDemoAnalyzer>();
        services.AddScoped<CounterStrikeMatchImporter>();
        services.AddScoped<CounterStrikeAwardService>();
        services.AddScoped<CounterStrikeCommunityService>();

        services.AddSingleton<IHighlightRule, MultiKillHighlightRule>();
        services.AddSingleton<IHighlightRule, ClutchHighlightRule>();
        services.AddSingleton<IHighlightRule, SpecialKillHighlightRule>();
        services.AddSingleton<IHighlightRule, NinjaDefuseHighlightRule>();
        services.AddSingleton<ITrainingRecommendationRule, UtilityTrainingRecommendationRule>();
        services.AddSingleton<ITrainingRecommendationRule, FirstDuelTrainingRecommendationRule>();
        services.AddSingleton<ITrainingRecommendationRule, PrecisionTrainingRecommendationRule>();
        services.AddSingleton<ITrainingRecommendationRule, TradingTrainingRecommendationRule>();
        services.AddSingleton<ICounterStrikeAwardRule, MvpAwardRule>();
        services.AddSingleton<ICounterStrikeAwardRule, EntryKingAwardRule>();
        services.AddSingleton<ICounterStrikeAwardRule, ClutchKingAwardRule>();
        services.AddSingleton<ICounterStrikeAwardRule, HeadshotKingAwardRule>();
        services.AddSingleton<ICounterStrikeAwardRule, UtilityMasterAwardRule>();
        services.AddSingleton<ICounterStrikeAwardRule, DeathCollectorAwardRule>();
        return services;
    }
}
