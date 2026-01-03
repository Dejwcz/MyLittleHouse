using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MujDomecek.Application.Abstractions;
using MujDomecek.Domain.Abstractions;
using MujDomecek.Domain.Aggregates.Admin;
using MujDomecek.Domain.Aggregates.Audit;
using MujDomecek.Domain.Aggregates.Notifications;
using MujDomecek.Domain.Aggregates.Project;
using MujDomecek.Domain.Aggregates.Property;
using MujDomecek.Domain.Aggregates.Sync;
using MujDomecek.Domain.Aggregates.User;
using MujDomecek.Domain.Aggregates.Zaznam;

namespace MujDomecek.Infrastructure.Persistence;

public sealed class ApplicationDbContext
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>, IApplicationDbContext, IUnitOfWork
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ContactGroup> ContactGroups => Set<ContactGroup>();
    public DbSet<ContactGroupMember> ContactGroupMembers => Set<ContactGroupMember>();

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<Invitation> Invitations => Set<Invitation>();

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<PropertyMember> PropertyMembers => Set<PropertyMember>();
    public DbSet<Activity> Activities => Set<Activity>();

    public DbSet<Zaznam> Zaznamy => Set<Zaznam>();
    public DbSet<ZaznamDokument> ZaznamDokumenty => Set<ZaznamDokument>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ZaznamTag> ZaznamTags => Set<ZaznamTag>();
    public DbSet<ZaznamMember> ZaznamMembers => Set<ZaznamMember>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<CommentMention> CommentMentions => Set<CommentMention>();

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SyncQueueItem> SyncQueueItems => Set<SyncQueueItem>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditInfo()
    {
        var now = DateTime.UtcNow;
        var revision = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not AuditableEntity<Guid> auditable)
                continue;

            if (entry.State == EntityState.Added)
            {
                if (auditable.CreatedAt == default)
                    auditable.CreatedAt = now;
                if (auditable.UpdatedAt == default)
                    auditable.UpdatedAt = now;

                auditable.ServerRevision = revision++;
            }
            else if (entry.State == EntityState.Modified)
            {
                if (auditable.UpdatedAt == default)
                    auditable.UpdatedAt = now;

                auditable.ServerRevision = revision++;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
            d => DateTime.SpecifyKind(d.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
            d => DateOnly.FromDateTime(d));

        builder.Entity<AppUser>()
            .HasQueryFilter(u => !u.IsDeleted);
        builder.Entity<AppUser>()
            .HasOne(u => u.Preferences)
            .WithOne()
            .HasForeignKey<UserPreferences>(p => p.UserId);

        builder.Entity<Project>()
            .HasQueryFilter(p => !p.IsDeleted);

        builder.Entity<Property>()
            .HasQueryFilter(p => !p.IsDeleted);

        builder.Entity<Unit>()
            .HasQueryFilter(u => !u.IsDeleted);

        builder.Entity<Zaznam>()
            .HasQueryFilter(z => !z.IsDeleted);

        builder.Entity<ZaznamDokument>()
            .HasQueryFilter(d => !d.IsDeleted);

        builder.Entity<Comment>()
            .HasQueryFilter(c => !c.IsDeleted);

        builder.Entity<Zaznam>()
            .Property(z => z.Date)
            .HasConversion(dateOnlyConverter);

        builder.Entity<ZaznamTag>()
            .HasKey(x => new { x.ZaznamId, x.TagId });

        builder.Entity<CommentMention>()
            .HasKey(x => new { x.CommentId, x.MentionedUserId });

        builder.Entity<AppSetting>()
            .HasKey(x => x.Key);

        builder.Entity<UserPreferences>()
            .HasKey(p => p.UserId);

        builder.Entity<PasswordResetToken>()
            .HasIndex(p => p.TokenHash)
            .IsUnique();

        builder.Entity<ProjectMember>()
            .HasIndex(x => new { x.ProjectId, x.UserId })
            .IsUnique();

        builder.Entity<PropertyMember>()
            .HasIndex(x => new { x.PropertyId, x.UserId })
            .IsUnique();

        builder.Entity<ZaznamMember>()
            .HasIndex(x => new { x.ZaznamId, x.UserId })
            .IsUnique();

        builder.Entity<Invitation>()
            .HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.Entity<SyncQueueItem>()
            .HasIndex(x => new { x.Status, x.NextRetryAt });
    }
}
