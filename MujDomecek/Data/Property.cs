using System.CodeDom;

namespace MujDomecek.Data {
    public class Property {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Reference to user
        public string UserId { get; set; }

        // Navigation property
        public ICollection<Unit> Units { get; set; }
    }
}
