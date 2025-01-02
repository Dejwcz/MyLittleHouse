namespace MujDomecek.Data; 
public class Unit {
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public UnitType UnitType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Reference to property
    public int PropertyId { get; set; }

    // Reference to parent unit
    public int? ParentUnitId { get; set; }

    // Navigation property
    public Unit ParentUnit { get; set; }
    public ICollection<Unit> ChildUnits { get; set; }

    public ICollection<Repair> Repairs { get; set; }
}