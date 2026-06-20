using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Infrastructure.Data.Projections.Collab;

namespace Notrelix.Infrastructure.Data.Configurations.Collaboration;

public class UnreadCounterConfiguration : IEntityTypeConfiguration<UnreadCounterRecord>
{
    public void Configure(EntityTypeBuilder<UnreadCounterRecord> builder)
    {
        builder.ToTable("unread_counters", DbSchemas.Collab);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.CounterType).HasColumnName("counter_type").IsRequired().HasMaxLength(80).HasDefaultValue("Notification");
        builder.Property(x => x.CounterValue).HasColumnName("counter_value").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(x => new { x.WorkspaceId, x.UserId, x.CounterType }).IsUnique().HasDatabaseName("ux_collab_unread_counters_user_type");
    }
}
