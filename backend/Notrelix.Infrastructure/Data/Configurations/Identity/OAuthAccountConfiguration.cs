using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Infrastructure.Data.Configurations.Identity;

public class OAuthAccountConfiguration : IEntityTypeConfiguration<OAuthAccount>
{
    public void Configure(EntityTypeBuilder<OAuthAccount> builder)
    {
        builder.ToTable("oauth_accounts", DbSchemas.Identity);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Provider).HasColumnName("provider").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.ProviderId).HasColumnName("provider_id").IsRequired().HasMaxLength(256);
        builder.Property(x => x.RawProfile).HasColumnName("raw_profile").HasColumnType("jsonb");

        builder.OwnsOne(x => x.Token, token =>
        {
            token.Property(t => t.ExpiresAt).HasColumnName("token_expires_at");
            token.Property(t => t.AccessTokenRef).HasColumnName("access_token_ref").HasConversion<Infrastructure.Data.Converters.SecretRefConverter>();
            token.Property(t => t.RefreshTokenRef).HasColumnName("refresh_token_ref").HasConversion<Infrastructure.Data.Converters.SecretRefConverter>();
        });

        builder.HasIndex(x => new { x.Provider, x.ProviderId }).IsUnique().HasDatabaseName("idx_oauth_accounts_provider");
        builder.HasIndex(x => x.UserId).HasDatabaseName("idx_oauth_accounts_user_id");
    }
}
