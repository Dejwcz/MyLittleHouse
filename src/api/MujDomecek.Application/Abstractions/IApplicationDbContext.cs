using Microsoft.EntityFrameworkCore;
using MujDomecek.Domain.Aggregates.Admin;
using MujDomecek.Domain.Aggregates.Audit;
using MujDomecek.Domain.Aggregates.Notifications;
using MujDomecek.Domain.Aggregates.Project;
using MujDomecek.Domain.Aggregates.Property;
using MujDomecek.Domain.Aggregates.User;
using MujDomecek.Domain.Aggregates.Zaznam;

namespace MujDomecek.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<AppUser> Users { get; }
    DbSet<UserPreferences> UserPreferences { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Contact> Contacts { get; }
    DbSet<ContactGroup> ContactGroups { get; }
    DbSet<ContactGroupMember> ContactGroupMembers { get; }

    DbSet<Project> Projects { get; }
    DbSet<ProjectMember> ProjectMembers { get; }
    DbSet<Invitation> Invitations { get; }

    DbSet<Property> Properties { get; }
    DbSet<Unit> Units { get; }
    DbSet<PropertyMember> PropertyMembers { get; }
    DbSet<Activity> Activities { get; }

    DbSet<Zaznam> Zaznamy { get; }
    DbSet<Media> Media { get; }
    DbSet<Tag> Tags { get; }
    DbSet<ZaznamTag> ZaznamTags { get; }
    DbSet<Comment> Comments { get; }
    DbSet<CommentMention> CommentMentions { get; }

    DbSet<Notification> Notifications { get; }
    DbSet<AppSetting> AppSettings { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
