using LeoHR.Server.Features.Activities;
using LeoHR.Server.Features.AppSettings;
using LeoHR.Server.Features.EmailTemplates;
using LeoHR.Server.Features.EmployeeActivities;
using LeoHR.Server.Features.Employees;
using LeoHR.Server.Features.KnowledgeEvaluations;
using LeoHR.Server.Features.Notifications;
using LeoHR.Server.Features.OnboardingFlows;
using LeoHR.Server.Features.OnboardingSettings;
using LeoHR.Server.Features.OnboardingTemplates;
using LeoHR.Server.Features.OnboardingTypes;
using LeoHR.Server.Features.PromotionCriteria;
using LeoHR.Server.Features.Promotions;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Email;
using LeoHR.Server.Shared.EmployeeActivities;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.Onboarding;
using LeoHR.Server.Shared.Promotions;

using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

using Npgsql;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Load environment-specific secrets (gitignored, never committed)
builder.Configuration.AddJsonFile(
    $"secrets.{builder.Environment.EnvironmentName}.json",
    optional: true,
    reloadOnChange: false);

// Add services to the container.

// Employee data from the Employee API (api.employee.leobit.co) — OAuth2 client-credentials + cache.
builder.Services.AddEmployeeApi(builder.Configuration);

// LeoHR data (PostgreSQL)
NpgsqlDataSource leoHrDataSource = new NpgsqlDataSourceBuilder(
        builder.Configuration.GetConnectionString("LeoHr"))
    .EnableDynamicJson()
    .Build();

builder.Services.AddDbContext<LeoHrDbContext>(o =>
    o.UseNpgsql(leoHrDataSource)
     .UseSnakeCaseNamingConvention());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IEmployeesService, EmployeesService>();
builder.Services.AddScoped<IEmployeeLookup, EmployeeLookup>();
builder.Services.AddScoped<IEmployeeLevelStart, EmployeeLevelStartService>();
builder.Services.AddScoped<IPromotionsService, PromotionsService>();
builder.Services.AddScoped<IPromotionCriteriaHistory, PromotionCriteriaHistoryService>();
builder.Services.AddScoped<PromotionCriteriaTicksService>();
builder.Services.AddScoped<IPromotionCriteriaTicks, PromotionCriteriaTicksService>();
builder.Services.AddScoped<IPromotionKnowledgeEvaluation, PromotionKnowledgeEvaluationService>();
builder.Services.AddScoped<IKnowledgeEvaluationsService, KnowledgeEvaluationsService>();
builder.Services.Configure<PromotionsOptions>(builder.Configuration.GetSection(PromotionsOptions.SectionName));
builder.Services.AddHostedService<MissingPromotionsJob>();
builder.Services.AddHostedService<PromotionReadyEmailJob>();
builder.Services.AddScoped<AppSettingsService>();
builder.Services.AddScoped<IAppSettingsFeatureService>(sp => sp.GetRequiredService<AppSettingsService>());
builder.Services.AddScoped<IEmailTemplatesService, EmailTemplatesService>();
builder.Services.AddScoped<IActivitiesService, ActivitiesService>();
builder.Services.AddScoped<IPromotionCriteriaService, PromotionCriteriaService>();
builder.Services.AddScoped<IEmployeeActivitiesService, EmployeeActivitiesService>();
builder.Services.AddScoped<IActivityVersions, ActivityVersionsService>();
builder.Services.AddScoped<IPromotionActivities, PromotionActivitiesService>();
builder.Services.AddScoped<IOnboardingSettingsService, OnboardingSettingsService>();
builder.Services.AddScoped<IOnboardingTemplatesService, OnboardingTemplatesService>();
builder.Services.AddScoped<IOnboardingTypesService, OnboardingTypesService>();
builder.Services.AddScoped<IOnboardingFlowsService, OnboardingFlowsService>();
builder.Services.AddScoped<IOnboardingCandidates, OnboardingCandidatesService>();
builder.Services.AddScoped<IOnboardingTypeArchive, OnboardingTypeArchiveService>();
builder.Services.AddScoped<IOnboardingFlowWarnings, OnboardingFlowWarningsService>();
builder.Services.AddScoped<INotificationsService, NotificationsService>();
builder.Services.AddFeatureManagement();
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddLeoHRAuthentication(builder.Configuration);
builder.Services.AddEmailSending(builder.Configuration);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(nameof(Policies.Manage), Policies.Manage);
    options.AddPolicy(nameof(Policies.ManageRead), Policies.ManageRead);
    options.AddPolicy(nameof(Policies.ManageAdmin), Policies.ManageAdmin);
    options.AddPolicy(nameof(Policies.Default), Policies.Default);
    options.AddPolicy(nameof(Policies.OnboardingTasks), Policies.OnboardingTasks);
    options.AddPolicy(nameof(Policies.OnboardingWrite), Policies.OnboardingWrite);
    options.AddPolicy(nameof(Policies.OnboardingAdmin), Policies.OnboardingAdmin);
    options.AddPolicy(nameof(Policies.Integrations), Policies.Integrations);
    options.DefaultPolicy = Policies.Default;
    // An endpoint without its own rule requires sign-in; public ones say AllowAnonymous.
    options.FallbackPolicy = Policies.Default;
});
string[] corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    LeoHrDbContext db = scope.ServiceProvider.GetRequiredService<LeoHrDbContext>();
    await db.Database.MigrateAsync();
}

app.UseDefaultFiles();
app.MapStaticAssets().AllowAnonymous();

// Configure the HTTP request pipeline.
if (!app.Environment.IsProduction())
{
    // Swagger UI fetches the document without a token.
    app.MapOpenApi().AllowAnonymous();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "LeoHR API"));
}

app.UseHttpsRedirection();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Intentionally does not probe the Employee API: it is monitored separately and LeoHR runs from cache when it is down.
app.MapGet("/api/health/db", async (LeoHrDbContext db) =>
    await db.Database.CanConnectAsync() ? Results.Ok("Connected") : Results.Problem("Failed"))
    .AllowAnonymous();

app.MapFallbackToFile("/index.html").AllowAnonymous();

app.Run();



