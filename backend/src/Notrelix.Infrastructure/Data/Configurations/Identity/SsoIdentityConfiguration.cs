using Notrelix.Domain.Identity.Sso;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Infrastructure.Data.Configurations.Identity;

public class SsoIdentityConfiguration : IEntityTypeConfiguration<SsoIdentity>
{
    public void Configure(EntityTypeBuilder<SsoIdentity> builder)
    {
        builder.ToTable("sso_identities", DbSchemas.Identity);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.Issuer).HasColumnName("issuer").IsRequired().HasMaxLength(512);
        builder.Property(x => x.Subject).HasColumnName("subject").IsRequired().HasMaxLength(256);
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.AccountId, x.Subject }).IsUnique().HasDatabaseName("idx_sso_identities_account_subject");
        builder.HasIndex(x => new { x.UserId, x.AccountId }).IsUnique().HasDatabaseName("idx_sso_identities_user_account");
    }
}
