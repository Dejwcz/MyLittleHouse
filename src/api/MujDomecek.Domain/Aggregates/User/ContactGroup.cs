namespace MujDomecek.Domain.Aggregates.User;

public sealed class ContactGroup
{
    public Guid Id { get; set; }

    public Guid OwnerUserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
