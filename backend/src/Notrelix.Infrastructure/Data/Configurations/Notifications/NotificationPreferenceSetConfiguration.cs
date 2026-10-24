using System.Text.Json;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.Data.Configurations.Notifications;

/// <summary>
/// Maps the Collaboration notification preference matrix: one <c>notification_preference_sets</c> row per
/// person and Workspace (the version and the scope) and the person's explicit choices in the existing
/// <c>notification_preferences</c> table. The columns of that table that the matrix does not use yet
/// (delivery mode, digest interval, quiet hours, time zone) stay as shadow properties so the table is only
/// extended, never reshaped.
/// </summary>
public sealed class NotificationPreferenceSetConfiguration : IEntityTypeConfiguration<NotificationPreferenceSet>
{
    public void Configure(EntityTypeBuilder<NotificationPreferenceSet> builder)
    {
        builder.ToTable("notification_preference_sets", DbSchemas.Notifications);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasMany(x => x.Entries)
            .WithOne()
            .HasForeignKey(x => x.SetId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Entries).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => new { x.WorkspaceId, x.UserId }).IsUnique().HasDatabaseName("ux_notification_preference_sets_member");
    }
}

public sealed class NotificationPreferenceEntryConfiguration : IEntityTypeConfiguration<NotificationPreferenceEntry>
{
    public void Configure(EntityTypeBuilder<NotificationPreferenceEntry> builder)
    {
        builder.ToTable("notification_preferences", DbSchemas.Notifications);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.SetId).HasColumnName("set_id");
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.NotificationType).HasColumnName("notification_type").IsRequired().HasMaxLength(160);
        builder.Property(x => x.Channel).HasColumnName("channel").IsRequired().HasMaxLength(50);
        builder.Property(x => x.IsEnabled).HasColumnName("is_enabled").IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        // Existing columns the matrix does not use yet (quiet hours and digest delivery are later decisions).
        builder.Property<DeliveryMode>("DeliveryMode").HasColumnName("delivery_mode").HasConversion<string>().IsRequired().HasMaxLength(50).HasDefaultValue(DeliveryMode.Immediate);
        builder.Property<int?>("DigestIntervalMinutes").HasColumnName("digest_interval_minutes");
        builder.Property<JsonDocument>("QuietHoursJson").HasColumnName("quiet_hours_json").HasColumnType("jsonb").IsRequired().HasDefaultValueSql("'{}'::jsonb");
        builder.Property<string?>("Timezone").HasColumnName("timezone").HasMaxLength(80);

        builder.HasIndex(x => new { x.UserId, x.NotificationType, x.Channel }).IsUnique().HasDatabaseName("ux_notifications_preferences_global")
            .HasFilter("\"workspace_id\" IS NULL");
        builder.HasIndex(x => new { x.WorkspaceId, x.UserId, x.NotificationType, x.Channel }).IsUnique().HasDatabaseName("ux_notifications_preferences_workspace")
            .HasFilter("\"workspace_id\" IS NOT NULL");
        builder.HasIndex(x => new { x.UserId, x.WorkspaceId }).HasDatabaseName("ix_notifications_preferences_user");
    }
}
