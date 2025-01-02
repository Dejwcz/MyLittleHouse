namespace MujDomecek.DTO; 
public class UnitFullViewDto {
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public UnitType UnitType { get; set; }
    public int PropertyId { get; set; }
    public string? ParentUnitName { get; set; }
    public ICollection<string> ChildUnitsNames { get; set; } = new List<string>();
    public ICollection<RepairDto> Repairs { get; set; } = new List<RepairDto>();
}
