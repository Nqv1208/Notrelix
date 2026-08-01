using Notrelix.Domain.Identity.Mfa;

namespace Notrelix.Infrastructure.Data.Configurations.Identity;

public class UserMfaMethodConfiguration : IEntityTypeConfiguration<UserMfaMethod>
{
    public void Configure(EntityTypeBuilder<UserMfaMethod> builder)
    {
        builder.ToTable("user_mfa_methods", DbSchemas.Identity);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.SecretRef).HasColumnName("secret_ref");
        builder.Property(x => x.DestinationMasked).HasColumnName("destination_masked").HasMaxLength(256);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.IsPrimary).HasColumnName("is_primary");
        builder.Property(x => x.VerifiedAt).HasColumnName("verified_at");
        builder.Property(x => x.DisabledAt).HasColumnName("disabled_at");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.UserId).HasDatabaseName("idx_user_mfa_methods_user_id");
    }
}
