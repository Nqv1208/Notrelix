using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Identity.Security;

namespace Notrelix.Infrastructure.Data.Configurations.Identity;

public class UserSecuritySettingsConfiguration : IEntityTypeConfiguration<UserSecuritySettings>
{
    public void Configure(EntityTypeBuilder<UserSecuritySettings> builder)
    {
        builder.ToTable("user_security_settings", DbSchemas.Identity);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.IsMfaEnabled).HasColumnName("is_mfa_enabled");
        builder.Property(x => x.PreferredMfaMethod).HasColumnName("preferred_mfa_method").HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.RequirePasswordChange).HasColumnName("require_password_change");
        builder.Property(x => x.PasswordChangedAt).HasColumnName("password_changed_at");
        builder.Property(x => x.LastSecurityReviewAt).HasColumnName("last_security_review_at");
        builder.Property(x => x.SettingsJson).HasColumnName("settings").HasColumnType("jsonb").IsRequired();

        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.RestoredAt).HasColumnName("restored_at");
        builder.Property(x => x.RestoredBy).HasColumnName("restored_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("idx_user_security_settings_user_id");
    }
}
