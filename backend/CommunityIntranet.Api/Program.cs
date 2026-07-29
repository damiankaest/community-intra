using System.Globalization;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using CommunityIntranet.Modules.AiAssistant;
using CommunityIntranet.Modules.AiAssistant.Endpoints;
using CommunityIntranet.Api.Endpoints;
using CommunityIntranet.Api.Infrastructure;
using CommunityIntranet.Infrastructure;
using CommunityIntranet.Infrastructure.Persistence;
using CommunityIntranet.Modules.Identity;
using CommunityIntranet.Modules.Identity.Endpoints;
using CommunityIntranet.Modules.ActivityFeed;
using CommunityIntranet.Modules.ActivityFeed.Endpoints;
using CommunityIntranet.Modules.Awards;
using CommunityIntranet.Modules.Awards.Endpoints;
using CommunityIntranet.Modules.Incidents;
using CommunityIntranet.Modules.Incidents.Endpoints;
using CommunityIntranet.Modules.LiveOperations;
using CommunityIntranet.Modules.LiveOperations.Endpoints;
using CommunityIntranet.Modules.Members;
using CommunityIntranet.Modules.Members.Endpoints;
using CommunityIntranet.Modules.Notifications;
using CommunityIntranet.Modules.Notifications.Endpoints;
using CommunityIntranet.Modules.Organizations;
using CommunityIntranet.Modules.Organizations.Endpoints;
using CommunityIntranet.Modules.Projects;
using CommunityIntranet.Modules.Projects.Endpoints;
using CommunityIntranet.Modules.Tasks;
using CommunityIntranet.Modules.Tasks.Endpoints;
using CommunityIntranet.Modules.ThemePacks;
using CommunityIntranet.Modules.ThemePacks.Endpoints;
using CommunityIntranet.Modules.TimeTracking;
using CommunityIntranet.Modules.TimeTracking.Endpoints;
using CommunityIntranet.Modules.FactoryInsights;
using CommunityIntranet.Modules.FactoryInsights.Endpoints;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Community Intranet API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance = context.HttpContext.Request.Path;
            context.ProblemDetails.Extensions["traceId"] =
                context.HttpContext.TraceIdentifier;
        };
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc(
            "v1",
            new OpenApiInfo
            {
                Title = "Community Intranet API",
                Version = "v1",
                Description = "API for the generic, multi-tenant Community Intranet platform."
            });
        options.AddSecurityDefinition(
            JwtBearerDefaults.AuthenticationScheme,
            new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT",
                In = ParameterLocation.Header
            });
        options.AddSecurityRequirement(
            new OpenApiSecurityRequirement
            {
                [
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = JwtBearerDefaults.AuthenticationScheme
                        }
                    }
                ] = []
            });
    });

    builder.Services.AddValidatorsFromAssemblyContaining<Program>();
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddCommunityIntranetInfrastructure(builder.Configuration);
    builder.Services.AddIdentityModule<CommunityIntranetDbContext>(
        builder.Configuration);
    builder.Services.AddOrganizationsModule();
    builder.Services.AddMembersModule();
    builder.Services.AddThemePacksModule();
    builder.Services.AddProjectsModule();
    builder.Services.AddTasksModule();
    builder.Services.AddIncidentsModule();
    builder.Services.AddAwardsModule();
    builder.Services.AddActivityFeedModule();
    builder.Services.AddAiAssistantModule(builder.Configuration);
    builder.Services.AddNotificationsModule();
    builder.Services.AddLiveOperationsModule(builder.Configuration);
    builder.Services.AddTimeTrackingModule();
    builder.Services.AddFactoryInsightsModule(builder.Configuration);
    builder.Services.AddScoped<DatabaseInitializer>();
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy(
            "authentication",
            httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
        options.AddPolicy(
            "invitations",
            httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
        options.AddPolicy(
            "assistant",
            httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirstValue("sub")
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    });

    builder.Services
        .AddHealthChecks()
        .AddDbContextCheck<CommunityIntranetDbContext>(
            "postgresql",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["ready"]);

    var allowedOrigins =
        builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(
            CorsPolicies.Frontend,
            policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins);
                }
                else
                {
                    policy.SetIsOriginAllowed(_ => false);
                }

                policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            });
    });

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();
    app.UseCors(CorsPolicies.Frontend);
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    if (app.Configuration.GetValue("Database:ApplyMigrations", false))
    {
        await using var scope = app.Services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync(CancellationToken.None);
    }

    app.MapSystemEndpoints();
    app.MapIdentityEndpoints();
    app.MapOrganizationEndpoints();
    app.MapMemberEndpoints();
    app.MapThemePackEndpoints();
    app.MapProjectEndpoints();
    app.MapTaskEndpoints();
    app.MapIncidentEndpoints();
    app.MapAwardEndpoints();
    app.MapActivityFeedEndpoints();
    app.MapAiAssistantEndpoints();
    app.MapNotificationEndpoints();
    app.MapLiveOperationsEndpoints();
    app.MapTimeTrackingEndpoints();
    app.MapFactoryInsightsEndpoints();

    await app.RunAsync();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Community Intranet API terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
