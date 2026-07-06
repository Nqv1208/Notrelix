using Notrelix.Infrastructure.Data.Projections.Search;

namespace Notrelix.Infrastructure.Data.Configurations.Search;

public class SearchIndexJobConfiguration : IEntityTypeConfiguration<SearchIndexJobRecord>
{
    public void Configure(EntityTypeBuilder<SearchIndexJobRecord> builder)
    {
        builder.ToTable("search_index_jobs", DbSchemas.Search);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.ResourceType).HasColumnName("resource_type").IsRequired().HasMaxLength(80);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id").IsRequired();
        builder.Property(x => x.Operation).HasColumnName("operation").IsRequired().HasMaxLength(40);
        builder.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(40).HasDefaultValue("Pending");
        builder.Property(x => x.Priority).HasColumnName("priority").IsRequired().HasDefaultValue(100);
        builder.Property(x => x.AttemptCount).HasColumnName("attempt_count").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.MaxAttempts).HasColumnName("max_attempts").IsRequired().HasDefaultValue(5);
        builder.Property(x => x.AvailableAt).HasColumnName("available_at").IsRequired();
        builder.Property(x => x.LockedBy).HasColumnName("locked_by").HasMaxLength(120);
        builder.Property(x => x.LockedUntil).HasColumnName("locked_until");
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id");
        builder.Property(x => x.CausationId).HasColumnName("causation_id");
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message");
        builder.Property(x => x.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.ProcessedAt).HasColumnName("processed_at");

        builder.HasIndex(x => new { x.Status, x.Priority, x.AvailableAt, x.CreatedAt }).HasDatabaseName("ix_search_index_jobs_pending");
        builder.HasIndex(x => x.LockedUntil).HasDatabaseName("ix_search_index_jobs_locks");
        builder.HasIndex(x => new { x.WorkspaceId, x.ResourceType, x.ResourceId, x.CreatedAt }).IsDescending(false, false, false, true).HasDatabaseName("ix_search_index_jobs_resource");
    }
}
