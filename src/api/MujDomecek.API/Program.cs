using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MujDomecek.API.Endpoints;
using MujDomecek.API.Jobs;
using MujDomecek.API.Options;
using MujDomecek.API.Options.Jobs;
using MujDomecek.API.Serialization;
using MujDomecek.API.Services;
using MujDomecek.Application.Abstractions;
using MujDomecek.Application;
using MujDomecek.Infrastructure;
using MujDomecek.Infrastructure.Persistence;
using Quartz;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) =>
{
    logger.ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console();
});

builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new DateOnlyJsonConverter());
});

// CORS for frontend development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173", "http://localhost:4173"];

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for HTTP-only cookies
    });
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<EmailTemplateService>();
builder.Services.AddSingleton<IEmailDispatcher, EmailDispatcher>();
builder.Services.AddScoped<ISyncQueueProcessor, SyncQueueProcessor>();

builder.Services.Configure<DraftCleanupOptions>(
    builder.Configuration.GetSection(DraftCleanupOptions.SectionName));
builder.Services.Configure<DraftReminderOptions>(
    builder.Configuration.GetSection(DraftReminderOptions.SectionName));
builder.Services.Configure<InvitationExpirationOptions>(
    builder.Configuration.GetSection(InvitationExpirationOptions.SectionName));
builder.Services.Configure<ActivityCleanupOptions>(
    builder.Configuration.GetSection(ActivityCleanupOptions.SectionName));
builder.Services.Configure<NotificationCleanupOptions>(
    builder.Configuration.GetSection(NotificationCleanupOptions.SectionName));
builder.Services.Configure<RefreshTokenCleanupOptions>(
    builder.Configuration.GetSection(RefreshTokenCleanupOptions.SectionName));
builder.Services.Configure<WeeklySummaryEmailOptions>(
    builder.Configuration.GetSection(WeeklySummaryEmailOptions.SectionName));
builder.Services.Configure<ExportDataOptions>(
    builder.Configuration.GetSection(ExportDataOptions.SectionName));
builder.Services.Configure<SyncRetryOptions>(
    builder.Configuration.GetSection(SyncRetryOptions.SectionName));

builder.Services.AddScoped<DraftCleanupJob>();
builder.Services.AddScoped<DraftReminderJob>();
builder.Services.AddScoped<InvitationExpirationJob>();
builder.Services.AddScoped<ActivityCleanupJob>();
builder.Services.AddScoped<NotificationCleanupJob>();
builder.Services.AddScoped<RefreshTokenCleanupJob>();
builder.Services.AddScoped<WeeklySummaryEmailJob>();
builder.Services.AddScoped<ExportDataJob>();
builder.Services.AddScoped<SyncRetryJob>();
builder.Services.AddScoped<SendEmailJob>();

builder.Services.AddQuartz(q =>
{

    var draftCleanupOptions = builder.Configuration
        .GetSection(DraftCleanupOptions.SectionName)
        .Get<DraftCleanupOptions>() ?? new DraftCleanupOptions();
    var draftReminderOptions = builder.Configuration
        .GetSection(DraftReminderOptions.SectionName)
        .Get<DraftReminderOptions>() ?? new DraftReminderOptions();
    var invitationExpirationOptions = builder.Configuration
        .GetSection(InvitationExpirationOptions.SectionName)
        .Get<InvitationExpirationOptions>() ?? new InvitationExpirationOptions();
    var activityCleanupOptions = builder.Configuration
        .GetSection(ActivityCleanupOptions.SectionName)
        .Get<ActivityCleanupOptions>() ?? new ActivityCleanupOptions();
    var notificationCleanupOptions = builder.Configuration
        .GetSection(NotificationCleanupOptions.SectionName)
        .Get<NotificationCleanupOptions>() ?? new NotificationCleanupOptions();
    var refreshTokenCleanupOptions = builder.Configuration
        .GetSection(RefreshTokenCleanupOptions.SectionName)
        .Get<RefreshTokenCleanupOptions>() ?? new RefreshTokenCleanupOptions();
    var weeklySummaryOptions = builder.Configuration
        .GetSection(WeeklySummaryEmailOptions.SectionName)
        .Get<WeeklySummaryEmailOptions>() ?? new WeeklySummaryEmailOptions();
    var syncRetryOptions = builder.Configuration
        .GetSection(SyncRetryOptions.SectionName)
        .Get<SyncRetryOptions>() ?? new SyncRetryOptions();

    AddCronJob<DraftCleanupJob>(
        q,
        "DraftCleanup",
        "DraftCleanupTrigger",
        NormalizeCron(draftCleanupOptions.CronSchedule, "0 0 3 * * ?"),
        draftCleanupOptions.Enabled);

    AddCronJob<DraftReminderJob>(
        q,
        "DraftReminder",
        "DraftReminderTrigger",
        NormalizeCron(draftReminderOptions.CronSchedule, "0 0 10 * * ?"),
        draftReminderOptions.Enabled);

    AddCronJob<InvitationExpirationJob>(
        q,
        "InvitationExpiration",
        "InvitationExpirationTrigger",
        NormalizeCron(invitationExpirationOptions.CronSchedule, "0 0 4 * * ?"),
        invitationExpirationOptions.Enabled);

    AddCronJob<ActivityCleanupJob>(
        q,
        "ActivityCleanup",
        "ActivityCleanupTrigger",
        NormalizeCron(activityCleanupOptions.CronSchedule, "0 0 2 ? * SUN"),
        activityCleanupOptions.Enabled);

    AddCronJob<NotificationCleanupJob>(
        q,
        "NotificationCleanup",
        "NotificationCleanupTrigger",
        NormalizeCron(notificationCleanupOptions.CronSchedule, "0 0 2 ? * SUN"),
        notificationCleanupOptions.Enabled);

    AddCronJob<RefreshTokenCleanupJob>(
        q,
        "RefreshTokenCleanup",
        "RefreshTokenCleanupTrigger",
        NormalizeCron(refreshTokenCleanupOptions.CronSchedule, "0 0 5 * * ?"),
        refreshTokenCleanupOptions.Enabled);

    AddCronJob<WeeklySummaryEmailJob>(
        q,
        "WeeklySummaryEmail",
        "WeeklySummaryEmailTrigger",
        NormalizeCron(weeklySummaryOptions.CronSchedule, "0 0 9 ? * MON"),
        weeklySummaryOptions.Enabled);

    AddCronJob<SyncRetryJob>(
        q,
        "SyncRetry",
        "SyncRetryTrigger",
        NormalizeCron(syncRetryOptions.CronSchedule, "0 */5 * * * ?"),
        syncRetryOptions.Enabled);
});

builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

var app = builder.Build();

app.UseSerilogRequestLogging();
if (app.Environment.IsEnvironment("Testing"))
    app.UseDeveloperExceptionPage();
else
    app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();

app.MapGet("/", () => "MujDomecek API");
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapProjectEndpoints();
app.MapPropertyEndpoints();
app.MapUnitEndpoints();
app.MapZaznamEndpoints();
app.MapCommentEndpoints();
app.MapContactEndpoints();
app.MapContactGroupEndpoints();
app.MapNotificationEndpoints();
app.MapMediaEndpoints();
app.MapUploadEndpoints();
app.MapSharingEndpoints();
app.MapInvitationEndpoints();
app.MapSyncEndpoints();
app.MapAdminEndpoints();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (app.Environment.IsEnvironment("Testing"))
        await dbContext.Database.EnsureCreatedAsync();
    else
        await dbContext.Database.MigrateAsync();

    await SeedData.SeedTagsAsync(dbContext, app.Configuration);
}

app.Run();

static void AddCronJob<TJob>(
    IServiceCollectionQuartzConfigurator configurator,
    string jobName,
    string triggerName,
    string cronSchedule,
    bool enabled)
    where TJob : IJob
{
    if (!enabled)
        return;

    var jobKey = new JobKey(jobName);
    configurator.AddJob<TJob>(options => options.WithIdentity(jobKey));
    configurator.AddTrigger(options => options
        .ForJob(jobKey)
        .WithIdentity(triggerName)
        .WithCronSchedule(cronSchedule));
}

static string NormalizeCron(string? cron, string fallback)
{
    var value = string.IsNullOrWhiteSpace(cron) ? fallback : cron.Trim();
    var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    return parts.Length == 5 ? $"0 {value}" : value;
}

public partial class Program { }
