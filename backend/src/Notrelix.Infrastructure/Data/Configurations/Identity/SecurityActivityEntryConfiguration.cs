using Notrelix.Domain.Identity.Security;

namespace Notrelix.Infrastructure.Data.Configurations.Identity;

public sealed class SecurityActivityEntryConfiguration : IEntityTypeConfiguration<SecurityActivityEntry>
{
    public void Configure(EntityTypeBuilder<SecurityActivityEntry> builder)
    {
        builder.ToTable("user_security_activity", DbSchemas.Identity);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Type).HasColumnName("event_type").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(SecurityActivityEntry.IpAddressMaxLength);
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(SecurityActivityEntry.UserAgentMaxLength);
        builder.Property(x => x.SessionId).HasColumnName("session_id");

        // Feed read: one user, newest first, keyset on (occurred_at, id).
        builder.HasIndex(x => new { x.UserId, x.OccurredAt, x.Id })
            .HasDatabaseName("idx_user_security_activity_user_feed")
            .IsDescending(false, true, true);

        // Retention sweep: oldest first across all users.
        builder.HasIndex(x => x.OccurredAt).HasDatabaseName("idx_user_security_activity_occurred_at");
    }
}
