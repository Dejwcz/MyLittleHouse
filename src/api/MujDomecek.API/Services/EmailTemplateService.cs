using System.Collections.ObjectModel;

namespace MujDomecek.API.Services;

public sealed record EmailTemplateResult(string Subject, string Body);

public sealed class EmailTemplateService
{
    private static readonly IReadOnlyDictionary<string, string> Subjects =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["email-confirmation"] = "Confirm your email - MujDomecek",
            ["password-reset"] = "Reset your password - MujDomecek",
            ["invitation"] = "You have been invited - MujDomecek",
            ["invitation-accepted"] = "Invitation accepted - MujDomecek",
            ["draft-reminder"] = "You have pending drafts - MujDomecek",
            ["weekly-summary"] = "Weekly summary - MujDomecek",
            ["export-ready"] = "Your data export is ready - MujDomecek"
        });

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<EmailTemplateService> _logger;

    public EmailTemplateService(IWebHostEnvironment environment, ILogger<EmailTemplateService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public EmailTemplateResult Render(string templateId, IReadOnlyDictionary<string, string?> tokens)
    {
        var subject = Subjects.TryGetValue(templateId, out var found)
            ? found
            : "MujDomecek";

        var body = RenderBody(templateId, tokens, subject);
        return new EmailTemplateResult(subject, body);
    }

    private string RenderBody(string templateId, IReadOnlyDictionary<string, string?> tokens, string subject)
    {
        var root = _environment.ContentRootPath;
        var templatesRoot = Path.Combine(root, "Templates", "Emails");
        var templatePath = Path.Combine(templatesRoot, $"{templateId}.html");
        var layoutPath = Path.Combine(templatesRoot, "_layout.html");

        if (!File.Exists(templatePath))
        {
            _logger.LogWarning("Email template not found: {TemplatePath}", templatePath);
            return BuildFallbackBody(tokens, subject);
        }

        var bodyTemplate = File.ReadAllText(templatePath);
        var body = ReplaceTokens(bodyTemplate, tokens);

        if (!File.Exists(layoutPath))
            return body;

        var layout = File.ReadAllText(layoutPath);
        var layoutTokens = new Dictionary<string, string?>(tokens, StringComparer.OrdinalIgnoreCase)
        {
            ["body"] = body,
            ["subject"] = subject
        };

        return ReplaceTokens(layout, layoutTokens);
    }

    private static string ReplaceTokens(string template, IReadOnlyDictionary<string, string?> tokens)
    {
        var output = template;
        foreach (var pair in tokens)
        {
            output = output.Replace(
                $"{{{{{pair.Key}}}}}",
                pair.Value ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        return output;
    }

    private static string BuildFallbackBody(IReadOnlyDictionary<string, string?> tokens, string subject)
    {
        var lines = new List<string>
        {
            subject,
            string.Empty
        };

        foreach (var pair in tokens)
            lines.Add($"{pair.Key}: {pair.Value}");

        return string.Join(Environment.NewLine, lines);
    }
}
