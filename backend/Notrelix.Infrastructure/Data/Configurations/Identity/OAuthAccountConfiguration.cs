using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Identity;

namespace Notrelix.Infrastructure.Data.Configurations.Identity;

public class OAuthAccountConfiguration : IEntityTypeConfiguration<OAuthAccount>
{
    public void Configure(EntityTypeBuilder<OAuthAccount> builder)
    {
        builder.ToTable("oauth_accounts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Provider)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ProviderId)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.AccessToken)
            .HasColumnType("text");

        builder.Property(e => e.RefreshToken)
            .HasColumnType("text");

        builder.Property(e => e.RawProfile)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

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
