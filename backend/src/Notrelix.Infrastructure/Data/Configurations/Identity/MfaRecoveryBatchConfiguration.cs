using Notrelix.Domain.Identity.Mfa;

namespace Notrelix.Infrastructure.Data.Configurations.Identity;

public class MfaRecoveryBatchConfiguration : IEntityTypeConfiguration<MfaRecoveryBatch>
{
    public void Configure(EntityTypeBuilder<MfaRecoveryBatch> builder)
    {
        builder.ToTable("mfa_recovery_batches", DbSchemas.Identity);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.InvalidatedAt).HasColumnName("invalidated_at");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.UserId).HasDatabaseName("idx_mfa_recovery_batches_user_id");
        builder.HasIndex(x => x.InvalidatedAt).HasDatabaseName("idx_mfa_recovery_batches_invalidated_at");

        builder.HasMany(b => b.Codes)
            .WithOne()
            .HasForeignKey("BatchId")
            .HasPrincipalKey(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(b => b.Codes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
