namespace MujDomecek.Application.DTOs;

public sealed record CommentDto(
    Guid Id,
    Guid ZaznamId,
    string Content,
    IReadOnlyList<MentionDto> Mentions,
    CommentAuthorDto Author,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsEdited
);

public sealed record MentionDto(Guid UserId, string Name);

public sealed record CommentAuthorDto(Guid Id, string Name, string? AvatarUrl);

public sealed record CreateCommentRequest(string Content);

public sealed record UpdateCommentRequest(string Content);
