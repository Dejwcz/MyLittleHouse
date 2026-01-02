namespace MujDomecek.Domain.Aggregates.Zaznam;

public sealed class Tag
{
    public short Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public short SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
