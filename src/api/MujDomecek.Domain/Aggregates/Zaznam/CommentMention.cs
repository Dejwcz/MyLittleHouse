namespace MujDomecek.Domain.Aggregates.Zaznam;

public sealed class CommentMention
{
    public Guid CommentId { get; set; }

    public Guid MentionedUserId { get; set; }
}
