namespace MujDomecek.DTO {
    public class PropertyDetailsDto {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public ICollection<string> UnitNames { get; set; }
        public int RepaisCount { get; set; }
        [Precision(18, 2)]
        public decimal? RepaisCost { get; set; }
    }
}
