using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Infrastructure.Data.Configurations.Identity;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", DbSchemas.Identity);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.OwnsOne(x => x.Email, email =>
        {
            email.Property(e => e.Value).HasColumnName("email").IsRequired().HasMaxLength(256);
        });

        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(256);
        builder.Property(x => x.Avatar).HasColumnName("avatar");
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.LastLoginAt).HasColumnName("last_login_at");

        builder.Ignore(x => x.AvatarUrl);

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasOne(x => x.Profile)
            .WithOne(x => x.User)
            .HasForeignKey<UserProfile>(x => x.UserId);

        builder.HasMany(x => x.Sessions)
            .WithOne()
            .HasForeignKey(x => x.UserId);

        builder.HasMany(x => x.OAuthAccounts)
            .WithOne()
            .HasForeignKey(x => x.UserId);

        builder.HasIndex(x => x.Email).IsUnique().HasDatabaseName("idx_users_email");
    }
}
