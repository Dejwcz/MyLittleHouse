namespace MujDomecek.Domain.Aggregates.User;

public sealed class Contact
{
    public Guid Id { get; set; }

    public Guid OwnerUserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public DateTime CreatedAt { get; set; }
}
