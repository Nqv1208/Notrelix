using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Integrations.Calendar;

namespace Notrelix.Infrastructure.Data.Configurations.Integrations;

public class CalendarIntegrationConfiguration : IEntityTypeConfiguration<CalendarIntegration>
{
    public void Configure(EntityTypeBuilder<CalendarIntegration> builder)
    {
        builder.ToTable("calendar_integrations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Provider).HasColumnName("provider").IsRequired().HasMaxLength(50);
        builder.Property(x => x.ProviderId).HasColumnName("provider_id").IsRequired().HasMaxLength(256);
        builder.Property(x => x.AccessToken).HasColumnName("access_token");
        builder.Property(x => x.RefreshToken).HasColumnName("refresh_token");
        builder.Property(x => x.TokenExpiresAt).HasColumnName("token_expires_at");
        builder.Property(x => x.SyncDirection).HasColumnName("sync_direction").HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(x => x.LastSyncAt).HasColumnName("last_sync_at");
        builder.Property(x => x.IsActive).HasColumnName("is_active");

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

        builder.HasIndex(x => x.UserId).HasDatabaseName("idx_calendar_integrations_user_id");
    }
}
