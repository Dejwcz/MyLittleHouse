using MujDomecek.Domain.Abstractions;

namespace MujDomecek.Domain.Aggregates.Zaznam;

public sealed class Comment : SoftDeletableEntity<Guid>
{
    public Guid ZaznamId { get; set; }

    public Guid AuthorUserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public List<CommentMention> Mentions { get; set; } = [];
}
