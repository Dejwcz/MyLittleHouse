namespace MujDomecek.Domain.Aggregates.User;

public sealed class ContactGroupMember
{
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }

    public Guid ContactId { get; set; }
}
