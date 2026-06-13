using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Identity.Profiles;

namespace Notrelix.Infrastructure.Data.Configurations.Identity;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles", DbSchemas.Identity);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Timezone).HasColumnName("timezone").IsRequired().HasMaxLength(64).HasDefaultValue("UTC");
        builder.Property(x => x.Locale).HasColumnName("locale").IsRequired().HasMaxLength(10).HasDefaultValue("vi");
        builder.Property(x => x.Theme).HasColumnName("theme").IsRequired().HasMaxLength(20).HasDefaultValue("system");
        builder.Property(x => x.Preferences).HasColumnName("preferences").HasColumnType("jsonb").IsRequired().HasDefaultValue("{}");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.User)
            .WithOne(x => x.Profile)
            .HasForeignKey<UserProfile>(x => x.UserId);

        builder.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("idx_user_profiles_user_id");
    }
}
