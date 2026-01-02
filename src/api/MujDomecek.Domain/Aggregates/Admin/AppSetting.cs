namespace MujDomecek.Domain.Aggregates.Admin;

public sealed class AppSetting
{
    public string Key { get; set; } = string.Empty;

    public string ValueJson { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }

    public Guid? UpdatedByUserId { get; set; }
}
