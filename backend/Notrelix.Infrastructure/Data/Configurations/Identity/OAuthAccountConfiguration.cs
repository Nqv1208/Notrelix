using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.Infrastructure.Data.Configurations.Identity;

public class OAuthAccountConfiguration : IEntityTypeConfiguration<OAuthAccount>
{
    public void Configure(EntityTypeBuilder<OAuthAccount> builder)
    {
        builder.ToTable("oauth_accounts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Provider).HasColumnName("provider").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.ProviderId).HasColumnName("provider_id").IsRequired().HasMaxLength(256);
        builder.Property(x => x.AccessToken).HasColumnName("access_token");
        builder.Property(x => x.RefreshToken).HasColumnName("refresh_token");
        builder.Property(x => x.TokenExpiresAt).HasColumnName("token_expires_at");

        builder.OwnsOne(x => x.RawProfile, profile =>
        {
            profile.Property(p => p.Value).HasColumnName("raw_profile").HasColumnType("jsonb").IsRequired();
        });

        builder.HasOne<User>()
            .WithMany(x => x.OAuthAccounts)
            .HasForeignKey(x => x.UserId);

        builder.HasIndex(x => new { x.Provider, x.ProviderId }).IsUnique().HasDatabaseName("idx_oauth_accounts_provider");
        builder.HasIndex(x => x.UserId).HasDatabaseName("idx_oauth_accounts_user_id");
    }
}
