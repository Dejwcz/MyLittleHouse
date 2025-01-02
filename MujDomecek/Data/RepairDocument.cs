namespace MujDomecek.Data {
    public class RepairDocument {
        public int Id { get; set; }
        public string FilePath { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Reference to repair
        public int RepairId { get; set; }
    }
}