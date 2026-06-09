using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Calendar;
using Notrelix.Domain.Enums;

namespace Notrelix.Infrastructure.Data.Configurations.Calendar;

public class CalendarIntegrationConfiguration : IEntityTypeConfiguration<CalendarIntegration>
{
    public void Configure(EntityTypeBuilder<CalendarIntegration> builder)
    {
        builder.ToTable("calendar_integrations");

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.WorkspaceId)
            .HasColumnName("workspace_id");

        builder.Property(e => e.Provider)
            .HasColumnName("provider")
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(e => e.ProviderAccountEmail)
            .HasColumnName("provider_account_email")
            .HasMaxLength(255);

        builder.Property(e => e.AccessToken)
            .HasColumnName("access_token")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.RefreshToken)
            .HasColumnName("refresh_token")
            .HasColumnType("text");

        builder.Property(e => e.TokenExpiresAt)
            .HasColumnName("token_expires_at");

        builder.Property(e => e.CalendarId)
            .HasColumnName("calendar_id")
            .HasMaxLength(500);

        builder.Property(e => e.SyncDirection)
            .HasColumnName("sync_direction")
            .HasConversion<string>()
            .HasMaxLength(10)
            .HasDefaultValue(SyncDirection.Both);

        builder.Property(e => e.LastSyncedAt)
            .HasColumnName("last_synced_at");

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.HasIndex(e => new { e.UserId, e.Provider })
            .IsUnique();

        builder.HasIndex(e => e.WorkspaceId);
    }
}
