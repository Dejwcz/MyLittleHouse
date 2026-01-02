using MujDomecek.Domain.Abstractions;

namespace MujDomecek.Domain.Aggregates.Project;

public sealed class Project : SoftDeletableEntity<Guid>
{
    public Guid OwnerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<ProjectMember> Members { get; set; } = [];
}
