using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Identity;

namespace Notrelix.Infrastructure.Data.Configurations.Identity;

public class OAuthAccountConfiguration : IEntityTypeConfiguration<OAuthAccount>
{
    public void Configure(EntityTypeBuilder<OAuthAccount> builder)
    {
        builder.ToTable("oauth_accounts");

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.Provider)
            .HasColumnName("provider")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ProviderId)
            .HasColumnName("provider_id")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.AccessToken)
            .HasColumnName("access_token")
            .HasColumnType("text");

        builder.Property(e => e.RefreshToken)
            .HasColumnName("refresh_token")
            .HasColumnType("text");

        builder.Property(e => e.TokenExpiresAt)
            .HasColumnName("token_expires_at");

        builder.Property(e => e.RawProfile)
            .HasColumnName("raw_profile")
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Unique: one user per provider
        builder.HasIndex(e => new { e.Provider, e.ProviderId })
            .IsUnique();

        builder.HasIndex(e => e.UserId);

        builder.HasOne(e => e.User)
            .WithMany(u => u.OAuthAccounts)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
