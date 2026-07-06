using Notrelix.Infrastructure.Data.Analytics;

namespace Notrelix.Infrastructure.Data.Configurations.Analytics;

public sealed class WorkspaceUsageDailyConfiguration : IEntityTypeConfiguration<WorkspaceUsageDaily>
{
    public void Configure(EntityTypeBuilder<WorkspaceUsageDaily> builder)
    {
        builder.ToTable("workspace_usage_daily", DbSchemas.Analytics);

        builder.HasKey(x => new { x.WorkspaceId, x.UsageDate });
        builder.Property(x => x.WorkspaceId).IsRequired();
        builder.Property(x => x.UsageDate).IsRequired();
        builder.Property(x => x.ActiveUsers).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.NewUsers).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.BoardsCreated).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.ItemsCreated).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.ItemsCompleted).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.DocsCreated).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.CommentsCreated).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.AutomationsExecuted).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.IntegrationsExecuted).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.StorageBytes).IsRequired().HasDefaultValue(0L);
        builder.Property(x => x.AttachmentCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.MetadataJson)
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'{}'::jsonb")
            .HasConversion<string>();
        builder.Ignore(x => x.MetadataJson);
        builder.Property(x => x.CalculatedAt).IsRequired();

        builder.HasIndex(x => x.UsageDate).IsDescending();
    }
}
