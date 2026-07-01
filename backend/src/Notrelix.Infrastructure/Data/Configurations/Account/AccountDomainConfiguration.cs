using Notrelix.Domain.Accounts.Domains;

namespace Notrelix.Infrastructure.Data.Configurations.Account;

public class AccountDomainConfiguration : IEntityTypeConfiguration<AccountDomain>
{
    public void Configure(EntityTypeBuilder<AccountDomain> builder)
    {
        builder.ToTable("account_domains", DbSchemas.Account);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.Domain).HasColumnName("domain").IsRequired().HasMaxLength(255);
        builder.Property(x => x.VerificationStatus).HasColumnName("verification_status").IsRequired().HasMaxLength(32);
        builder.Property(x => x.VerificationTokenHash).HasColumnName("verification_token_hash").HasMaxLength(255);
        builder.Property(x => x.VerifiedAt).HasColumnName("verified_at");
        builder.Property(x => x.AutoJoinEnabled).HasColumnName("auto_join_enabled");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(x => x.Domain).IsUnique().HasDatabaseName("idx_account_domains_domain");
    }
}
