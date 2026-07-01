using Notrelix.Infrastructure.Data.Analytics;

namespace Notrelix.Infrastructure.Data.Configurations.Analytics;

public sealed class FeatureUsageDailyConfiguration : IEntityTypeConfiguration<FeatureUsageDaily>
{
    public void Configure(EntityTypeBuilder<FeatureUsageDaily> builder)
    {
        builder.ToTable("feature_usage_daily", DbSchemas.Analytics);

        builder.HasKey(x => new { x.WorkspaceId, x.UsageDate, x.FeatureCode });
        builder.Property(x => x.WorkspaceId).IsRequired();
        builder.Property(x => x.UsageDate).IsRequired();
        builder.Property(x => x.FeatureCode).IsRequired().HasMaxLength(120);
        builder.Property(x => x.UsageCount).IsRequired().HasDefaultValue(0L);
        builder.Property(x => x.UniqueActorCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.Quantity).IsRequired().HasDefaultValue(0m);
        builder.Property(x => x.Unit).HasMaxLength(80);
        builder.Property(x => x.MetadataJson)
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'{}'::jsonb")
            .HasConversion<string>();
        builder.Ignore(x => x.MetadataJson);
        builder.Property(x => x.CalculatedAt).IsRequired();

        builder.HasIndex(x => new { x.FeatureCode, x.UsageDate }).IsDescending(false, true);
    }
}
