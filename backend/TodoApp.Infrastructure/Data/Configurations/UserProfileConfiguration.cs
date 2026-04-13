using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Domain.Entities;

namespace TodoApp.Infrastructure.Data.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");
        builder.HasKey(x => x.UserId);
        builder.Ignore(x => x.Id);

        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Timezone).HasColumnName("timezone").HasMaxLength(50).HasDefaultValue("UTC");
        builder.Property(x => x.Locale).HasColumnName("locale").HasMaxLength(10).HasDefaultValue("en");
        builder.Property(x => x.Preferences).HasColumnName("preferences").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.User)
            .WithOne(u => u.Profile)
            .HasForeignKey<UserProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}
