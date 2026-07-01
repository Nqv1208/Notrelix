using Notrelix.Domain.Integrations.Sync;
using Notrelix.Infrastructure.Data.Converters;

namespace Notrelix.Infrastructure.Data.Configurations.Integrations;

public class IntegrationSyncCursorConfiguration : IEntityTypeConfiguration<IntegrationSyncCursor>
{
    public void Configure(EntityTypeBuilder<IntegrationSyncCursor> builder)
    {
        builder.ToTable("integration_sync_cursors", DbSchemas.Integration);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.ConnectionId).HasColumnName("connection_id").IsRequired();
        builder.Property(x => x.ResourceType).HasColumnName("resource_type").IsRequired().HasMaxLength(100);
        builder.Property(x => x.Cursor).HasColumnName("cursor_value").HasConversion<SyncCursorValueConverter>().IsRequired().HasMaxLength(1024);
        builder.Property(x => x.LastSyncedAt).HasColumnName("last_synced_at").IsRequired();

        builder.HasIndex(x => new { x.ConnectionId, x.ResourceType }).HasDatabaseName("idx_integration_sync_cursors_connection_resource");
    }
}
