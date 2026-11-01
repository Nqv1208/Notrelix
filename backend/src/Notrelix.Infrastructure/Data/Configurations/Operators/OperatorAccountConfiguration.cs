using Notrelix.Domain.Operators;

namespace Notrelix.Infrastructure.Data.Configurations.Operators;

public class OperatorAccountConfiguration : IEntityTypeConfiguration<OperatorAccount>
{
    public void Configure(EntityTypeBuilder<OperatorAccount> builder)
    {
        builder.ToTable("operator_accounts", DbSchemas.Operators);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.Email).HasColumnName("email").IsRequired().HasMaxLength(320);
        builder.Property(x => x.DisplayName).HasColumnName("display_name").IsRequired().HasMaxLength(160);
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired().HasMaxLength(256);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(32);
        builder.Property(x => x.FailedSignInCount).HasColumnName("failed_sign_in_count").IsRequired();
        builder.Property(x => x.LockedUntil).HasColumnName("locked_until");
        builder.Property(x => x.LastSignedInAt).HasColumnName("last_signed_in_at");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.Email).IsUnique().HasDatabaseName("ux_operator_accounts_email");
    }
}
