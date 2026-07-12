using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.Data.Configurations.Notifications;

public sealed class EmailOutboxMessageConfiguration : IEntityTypeConfiguration<EmailOutboxMessage>
{
    public void Configure(EntityTypeBuilder<EmailOutboxMessage> builder)
    {
        builder.ToTable("email_outbox", DbSchemas.Notifications);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.DeduplicationKey).IsRequired().HasMaxLength(320);
        builder.HasIndex(x => x.DeduplicationKey).IsUnique();

        builder.Property(x => x.SourceContext).HasMaxLength(80);
        builder.Property(x => x.SourceEventId);
        builder.Property(x => x.SourceMessageId);
        builder.Property(x => x.WorkspaceId);
        builder.Property(x => x.RecipientUserId);
        builder.Property(x => x.RecipientEmail).IsRequired();
        builder.Property(x => x.RecipientName).HasMaxLength(240);
        builder.Property(x => x.ContentMode).HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.TemplateName).IsRequired().HasMaxLength(160);
        builder.Property(x => x.TemplateVersion).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.Subject).HasMaxLength(320);
        builder.Property(x => x.TemplateDataJson).HasColumnType("jsonb").HasConversion<string>();
        builder.Property(x => x.HeadersJson).HasColumnType("jsonb").HasConversion<string>().IsRequired().HasDefaultValueSql("'{}'::jsonb");
        builder.Property(x => x.Priority).IsRequired().HasDefaultValue(100);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(40).HasDefaultValue("Pending");
        builder.Property(x => x.RetryCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.MaxRetries).IsRequired().HasDefaultValue(5);
        builder.Property(x => x.NextAttemptAt).IsRequired();
        builder.Property(x => x.LockedBy).HasMaxLength(160);
        builder.Property(x => x.LockToken).HasMaxLength(80);
        builder.Property(x => x.Provider).HasMaxLength(120);
        builder.Property(x => x.ProviderMessageId).HasMaxLength(240);
        builder.Property(x => x.LastErrorCode).HasMaxLength(120);
        builder.Property(x => x.SensitivePayloadExpiresAt);
        builder.Property(x => x.SensitivePayloadClearedAt);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_email_outbox_content_mode",
                "(content_mode = 'Rendered' AND subject IS NOT NULL AND (body_html IS NOT NULL OR body_text IS NOT NULL) AND template_data_json IS NULL) OR (content_mode = 'Templated' AND subject IS NULL AND body_html IS NULL AND body_text IS NULL AND template_data_json IS NOT NULL AND template_data_json <> '{}'::jsonb)");
            table.HasCheckConstraint(
                "ck_email_outbox_sensitive_payload_state",
                "sensitive_payload_cleared_at IS NULL OR template_data_json IS NULL");
        });

        builder.HasIndex(x => new { x.Status, x.Priority, x.NextAttemptAt, x.CreatedAt })
            .HasFilter("\"status\" IN ('Pending', 'Failed')");
        builder.HasIndex(x => new { x.Status, x.LockedUntil, x.ProcessingStartedAt })
            .HasFilter("\"status\" = 'Sending'");
        builder.HasIndex(x => new { x.WorkspaceId, x.CreatedAt })
            .HasFilter("\"workspace_id\" IS NOT NULL")
            .IsDescending(false, true);
        builder.HasIndex(x => new { x.RecipientUserId, x.CreatedAt })
            .HasFilter("\"recipient_user_id\" IS NOT NULL")
            .IsDescending(false, true);
        builder.HasIndex(x => x.SourceMessageId)
            .HasFilter("\"source_message_id\" IS NOT NULL");
    }
}
