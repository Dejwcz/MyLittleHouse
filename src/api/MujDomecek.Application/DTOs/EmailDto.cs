namespace MujDomecek.Application.DTOs;

public sealed record EmailJobRequest(
    string To,
    string Subject,
    string Body,
    string? Template = null,
    IReadOnlyDictionary<string, string?>? Variables = null
);
