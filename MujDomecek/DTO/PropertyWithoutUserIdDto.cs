using System.ComponentModel.DataAnnotations;

namespace MujDomecek.DTO {
    public class PropertyWithoutUserIdDto {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string? Description { get; set; } = string.Empty;
    }
}
