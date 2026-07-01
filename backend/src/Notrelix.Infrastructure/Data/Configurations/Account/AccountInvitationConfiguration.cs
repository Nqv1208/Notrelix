using Notrelix.Domain.Accounts.Invitations;

namespace Notrelix.Infrastructure.Data.Configurations.Account;

public class AccountInvitationConfiguration : IEntityTypeConfiguration<AccountInvitation>
{
    public void Configure(EntityTypeBuilder<AccountInvitation> builder)
    {
        builder.ToTable("account_invitations", DbSchemas.Account);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").IsRequired().HasMaxLength(320);
        builder.Property(x => x.Role).HasColumnName("role").HasConversion<string>().IsRequired().HasMaxLength(40);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(32);
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(x => x.InvitedBy).HasColumnName("invited_by").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => new { x.AccountId, x.Email }).HasDatabaseName("idx_account_invitations_account_email");
    }
}
