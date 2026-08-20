using Notrelix.Domain.Identity.Mfa;

namespace Notrelix.Infrastructure.Data.Configurations.Identity;

public class MfaRecoveryCodeConfiguration : IEntityTypeConfiguration<MfaRecoveryCode>
{
    public void Configure(EntityTypeBuilder<MfaRecoveryCode> builder)
    {
        builder.ToTable("mfa_recovery_codes", DbSchemas.Identity);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.BatchId).HasColumnName("batch_id").IsRequired();
        builder.Property(x => x.CodeHash).HasColumnName("code_hash").IsRequired().HasMaxLength(64);
        builder.Property(x => x.ConsumedAt).HasColumnName("consumed_at");

        builder.HasIndex(x => new { x.BatchId, x.CodeHash }).HasDatabaseName("idx_mfa_recovery_codes_batch_code");
        builder.HasIndex(x => x.ConsumedAt).HasDatabaseName("idx_mfa_recovery_codes_consumed_at");
    }
}
