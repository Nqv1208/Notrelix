using Notrelix.Domain.Accounts.IdentityProviders;

namespace Notrelix.Infrastructure.Data.Configurations.Account;

public class AccountIdentityProviderConfiguration : IEntityTypeConfiguration<AccountIdentityProvider>
{
    public void Configure(EntityTypeBuilder<AccountIdentityProvider> builder)
    {
        builder.ToTable("account_identity_providers", DbSchemas.Account);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.ProviderType).HasColumnName("provider_type").IsRequired().HasMaxLength(32);
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(120);
        builder.Property(x => x.Issuer).HasColumnName("issuer").IsRequired().HasMaxLength(300);
        builder.Property(x => x.SsoUrl).HasColumnName("sso_url").IsRequired();
        builder.Property(x => x.CertificateRef).HasColumnName("certificate_ref").HasMaxLength(255);
        builder.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(32);
        builder.Property(x => x.JitProvisioningEnabled).HasColumnName("jit_provisioning_enabled");

        builder.Ignore(x => x.IsDeleted);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.RestoredAt).HasColumnName("restored_at");
        builder.Property(x => x.RestoredBy).HasColumnName("restored_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
    }
}
