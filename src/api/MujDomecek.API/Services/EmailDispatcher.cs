using System.Text.Json;
using MujDomecek.API.Jobs;
using MujDomecek.Application.Abstractions;
using MujDomecek.Application.DTOs;
using Quartz;

namespace MujDomecek.API.Services;

public sealed class EmailDispatcher : IEmailDispatcher
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailDispatcher> _logger;

    public EmailDispatcher(
        ISchedulerFactory schedulerFactory,
        IEmailSender emailSender,
        ILogger<EmailDispatcher> logger)
    {
        _schedulerFactory = schedulerFactory;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task SendAsync(EmailJobRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.To))
            return;

        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey($"SendEmail-{Guid.NewGuid():N}");
            var job = JobBuilder.Create<SendEmailJob>()
                .WithIdentity(jobKey)
                .UsingJobData("request", JsonSerializer.Serialize(request, SerializerOptions))
                .Build();

            var trigger = TriggerBuilder.Create()
                .ForJob(job)
                .StartNow()
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EmailDispatcher: falling back to direct send.");
            await _emailSender.SendAsync(request.To, request.Subject, request.Body, ct);
        }
    }
}
