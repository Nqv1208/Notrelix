using Notrelix.Domain.Operators;

namespace Notrelix.Infrastructure.Data.Configurations.Operators;

public class OperatorAuditEntryConfiguration : IEntityTypeConfiguration<OperatorAuditEntry>
{
    public void Configure(EntityTypeBuilder<OperatorAuditEntry> builder)
    {
        builder.ToTable("operator_audit", DbSchemas.Operators);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.OperatorId).HasColumnName("operator_id").IsRequired();
        builder.Property(x => x.Action).HasColumnName("action").IsRequired().HasMaxLength(64);
        builder.Property(x => x.TargetKind).HasColumnName("target_kind").IsRequired().HasMaxLength(32);
        builder.Property(x => x.TargetId).HasColumnName("target_id").IsRequired();
        builder.Property(x => x.AccountId).HasColumnName("account_id");
        builder.Property(x => x.Reason).HasColumnName("reason").IsRequired().HasMaxLength(OperatorAuditEntry.MaxReasonLength);
        builder.Property(x => x.BeforeJson).HasColumnName("before_json").HasColumnType("jsonb");
        builder.Property(x => x.AfterJson).HasColumnName("after_json").HasColumnType("jsonb");
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();

        builder.HasIndex(x => new { x.AccountId, x.OccurredAt }).HasDatabaseName("idx_operator_audit_account_time");
        builder.HasIndex(x => x.OccurredAt).HasDatabaseName("idx_operator_audit_time");
    }
}
