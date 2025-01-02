namespace MujDomecek.DTO; 
public class RepairDto {
    public int Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public int? Cost { get; set; }
    public DateOnly RepairDate { get; set; }
    public int UnitId { get; set; }
}
