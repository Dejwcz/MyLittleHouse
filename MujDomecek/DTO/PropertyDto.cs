using System.ComponentModel.DataAnnotations;

namespace MujDomecek.DTO {
    public class PropertyDto {    
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string? Description { get; set; } = string.Empty;
        public string UserId { get; set; }
    }
}
