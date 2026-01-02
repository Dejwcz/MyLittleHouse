using System.Text.Json;
using MujDomecek.API.Services;
using MujDomecek.Application.Abstractions;
using MujDomecek.Application.DTOs;
using Quartz;

namespace MujDomecek.API.Jobs;

public sealed class SendEmailJob : IJob
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IEmailSender _emailSender;
    private readonly EmailTemplateService _emailTemplates;
    private readonly ILogger<SendEmailJob> _logger;

    public SendEmailJob(
        IEmailSender emailSender,
        EmailTemplateService emailTemplates,
        ILogger<SendEmailJob> logger)
    {
        _emailSender = emailSender;
        _emailTemplates = emailTemplates;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var data = context.MergedJobDataMap;
        var request = ParseRequest(data);
        if (request is null || string.IsNullOrWhiteSpace(request.To))
            return;

        var subject = request.Subject;
        var body = request.Body;

        if (!string.IsNullOrWhiteSpace(request.Template))
        {
            var render = _emailTemplates.Render(
                request.Template,
                request.Variables ?? new Dictionary<string, string?>());

            if (string.IsNullOrWhiteSpace(subject))
                subject = render.Subject;
            if (string.IsNullOrWhiteSpace(body))
                body = render.Body;
        }

        if (string.IsNullOrWhiteSpace(subject))
            subject = "MujDomecek";
        if (string.IsNullOrWhiteSpace(body))
            body = subject;

        await _emailSender.SendAsync(request.To, subject, body, context.CancellationToken);
        _logger.LogInformation("SendEmail: sent to {Recipient}", request.To);
    }

    private static EmailJobRequest? ParseRequest(JobDataMap data)
    {
        var requestJson = data.GetString("request");
        if (!string.IsNullOrWhiteSpace(requestJson))
        {
            return JsonSerializer.Deserialize<EmailJobRequest>(requestJson, SerializerOptions);
        }

        var to = data.GetString("to");
        var subject = data.GetString("subject") ?? string.Empty;
        var body = data.GetString("body") ?? string.Empty;
        var template = data.GetString("template");
        IReadOnlyDictionary<string, string?>? variables = null;

        var variablesJson = data.GetString("variables");
        if (!string.IsNullOrWhiteSpace(variablesJson))
        {
            variables = JsonSerializer.Deserialize<Dictionary<string, string?>>(variablesJson, SerializerOptions);
        }

        if (string.IsNullOrWhiteSpace(to))
            return null;

        return new EmailJobRequest(to, subject, body, template, variables);
    }
}
