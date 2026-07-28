using CommunityIntranet.Api.Endpoints;
using CommunityIntranet.Api.Infrastructure;
using CommunityIntranet.Infrastructure;
using CommunityIntranet.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
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
    });

    builder.Services.AddValidatorsFromAssemblyContaining<Program>();
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddCommunityIntranetInfrastructure(builder.Configuration);
    builder.Services.AddScoped<DatabaseInitializer>();

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

                policy.AllowAnyHeader().AllowAnyMethod();
            });
    });

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();
    app.UseCors(CorsPolicies.Frontend);

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        if (app.Configuration.GetValue("Database:ApplyMigrations", true))
        {
            await using var scope = app.Services.CreateAsyncScope();
            var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
            await initializer.ApplyMigrationsAsync(CancellationToken.None);
        }
    }

    app.MapSystemEndpoints();

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
