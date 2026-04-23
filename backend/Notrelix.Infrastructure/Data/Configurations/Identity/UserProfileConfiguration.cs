using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Identity;

namespace Notrelix.Infrastructure.Data.Configurations.Identity;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");
        builder.HasKey(x => x.UserId);

        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Timezone).HasColumnName("timezone").HasMaxLength(50).HasDefaultValue("UTC");
        builder.Property(x => x.Locale).HasColumnName("locale").HasMaxLength(10).HasDefaultValue("vi");
        builder.Property(x => x.Theme).HasColumnName("theme").HasMaxLength(20).HasDefaultValue("system");
        builder.Property(x => x.Preferences).HasColumnName("preferences").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.User)
            .WithOne(u => u.Profile)
            .HasForeignKey<UserProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
