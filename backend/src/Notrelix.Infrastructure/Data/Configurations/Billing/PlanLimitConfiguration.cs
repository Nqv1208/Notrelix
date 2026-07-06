using Notrelix.Domain.Billing.Plans;

namespace Notrelix.Infrastructure.Data.Configurations.Billing;

public class PlanLimitConfiguration : IEntityTypeConfiguration<PlanLimit>
{
    public void Configure(EntityTypeBuilder<PlanLimit> builder)
    {
        builder.ToTable("plan_limits", DbSchemas.Billing);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.PlanId).HasColumnName("plan_id").IsRequired();
        builder.Property(x => x.Feature).HasColumnName("feature_code").HasConversion(v => v.Code, v => FeatureCode.Create(v)).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Limit).HasColumnName("limit_value").IsRequired();

        builder.HasIndex(x => x.PlanId).HasDatabaseName("idx_plan_limits_plan_id");
    }
}
