using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Governance.Security;

namespace Notrelix.Infrastructure.Data.Configurations.Governance;

public class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(EntityTypeBuilder<SecurityEvent> builder)
    {
        builder.ToTable("security_events");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.EventType).HasColumnName("event_type").IsRequired().HasMaxLength(100);
        builder.Property(x => x.Severity).HasColumnName("severity").HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(512);
        builder.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        builder.Property(x => x.Timestamp).HasColumnName("timestamp").IsRequired();

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_security_events_workspace_id");
        builder.HasIndex(x => x.Timestamp).HasDatabaseName("idx_security_events_timestamp");
    }
}
