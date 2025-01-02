using System.ComponentModel.DataAnnotations;

namespace MujDomecek.DTO;
public class RepairDetailViewDto {
    public int Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    [Range(0, int.MaxValue, ErrorMessage = "Cost must be a positive whole number.")]
    public int? Cost { get; set; }
    public DateOnly RepairDate { get; set; }
    public int UnitId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<RepairDocumentsDto> Documents { get; set; } = new List<RepairDocumentsDto>();
}
