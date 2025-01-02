namespace MujDomecek.DTO; 
public class PropertyFullViewDto {
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; } = string.Empty;
    public string UserId { get; set; }
    public ICollection<UnitFullViewDto> Units { get; set; } = new List<UnitFullViewDto>();

}
