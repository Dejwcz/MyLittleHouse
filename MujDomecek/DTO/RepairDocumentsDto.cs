namespace MujDomecek.DTO {
    public class RepairDocumentsDto {
        public int Id { get; set; }
        public string FilePath { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }    
        public int RepairId { get; set; }
    }
}
