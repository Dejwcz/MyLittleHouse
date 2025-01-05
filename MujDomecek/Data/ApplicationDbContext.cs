using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace MujDomecek.Data; 
public class ApplicationDbContext : IdentityDbContext<AppUser> {
    // acces to user-secrets
    private readonly IConfiguration _configuration;
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration) : base(options) {
        _configuration = configuration;
    }

    protected override void OnModelCreating(ModelBuilder builder) {
        base.OnModelCreating(builder);
        builder.Entity<IdentityRole>().HasData(
            new IdentityRole {
                Id = "c45f0814-7e4a-454b-98fb-61a534a6e19a",
                Name = "User",
                NormalizedName = "USER"
            },
            new IdentityRole {
                Id = "be221c97-926c-4ea0-886e-38134cdd90d2",
                Name = "Supervisor",
                NormalizedName = "SUPERVISOR",
            },
            new IdentityRole {
                Id = "45172101-2d51-4025-aa5d-f90eb130b904",
                Name = "DungeonMaster",
                NormalizedName = "DUNGEONMASTER"
            });

        builder.Entity<Property>().HasQueryFilter(p => !p.IsDeleted);
        builder.Entity<Unit>().HasQueryFilter(p => !p.IsDeleted);
        builder.Entity<Repair>().HasQueryFilter(p => !p.IsDeleted);
        builder.Entity<RepairDocument>().HasQueryFilter(p => !p.IsDeleted);

        builder.Entity<Unit>()
            .HasOne(u => u.ParentUnit) // Unit has one parent
            .WithMany(u => u.ChildUnits) // Unit can have many children
            .HasForeignKey(u => u.ParentUnitId) // Foreign key
            .OnDelete(DeleteBehavior.Restrict); // Delete restriction
    }

    public DbSet<Property> Properties { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<Repair> Repairs { get; set; }
    public DbSet<RepairDocument> RepairDocuments { get; set; }

}
