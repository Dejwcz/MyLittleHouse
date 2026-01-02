using Microsoft.Extensions.Logging;
using MujDomecek.Application.Abstractions;

namespace MujDomecek.Infrastructure.Services;

public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("Email to {Recipient} - {Subject}\n{Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
