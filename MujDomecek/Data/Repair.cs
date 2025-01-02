using Microsoft.EntityFrameworkCore;

namespace MujDomecek.Data {
    public class Repair {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int? Cost { get; set; }
        public DateOnly RepairDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Reference to unit
        public int UnitId { get; set; }

        // Navigation property
        public ICollection<RepairDocument> Documents { get; set; }
    }
}