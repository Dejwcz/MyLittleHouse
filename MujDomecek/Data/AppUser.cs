﻿using Microsoft.AspNetCore.Identity;

namespace MujDomecek.Data;
public class AppUser : IdentityUser {
    public string FirstName { get; set; }
    public string LastName { get; set; }

    // Navigation property
    public ICollection<Property> Properties { get; set; }
}
