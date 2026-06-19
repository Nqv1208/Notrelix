using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Governance.Security.Events;

namespace Notrelix.Infrastructure.Data.Configurations.Governance;

public class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(EntityTypeBuilder<SecurityEvent> builder)
    {
        builder.ToTable("security_events", DbSchemas.Governance);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.Type).HasColumnName("event_type").HasConversion<string>().IsRequired().HasMaxLength(100);
        builder.Property(x => x.Severity).HasColumnName("severity").HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(x => x.Title).HasColumnName("title").IsRequired().HasMaxLength(256);
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(1024);
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();

        builder.OwnsOne(x => x.Metadata, metadata =>
        {
            metadata.Property(m => m.Data).HasColumnName("metadata").HasColumnType("jsonb");
        });

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_security_events_workspace_id");
        builder.HasIndex(x => x.OccurredAt).HasDatabaseName("idx_security_events_occurred_at");
    }
}
