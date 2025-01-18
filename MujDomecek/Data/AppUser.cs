using Microsoft.AspNetCore.Identity;

namespace MujDomecek.Data;
public class AppUser : IdentityUser {
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }


    // Navigation property
    public ICollection<Property> Properties { get; set; }
}
