using Notrelix.Domain.Chat.Retention;

namespace Notrelix.Infrastructure.Data.Configurations.Chat;

/// <summary>
/// The retention rule of a Workspace's Chat (UC-CHT-H05): one row per Workspace, created when the Owner first sets a rule. A Workspace
/// without a row has no retention. The mode is stored as its wire text.
/// </summary>
public class ChatRetentionPolicyConfiguration : IEntityTypeConfiguration<RetentionPolicy>
{
    public void Configure(EntityTypeBuilder<RetentionPolicy> builder)
    {
        builder.ToTable("retention_policies", DbSchemas.Chat);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.RetentionDays).HasColumnName("retention_days");
        builder.Property(x => x.Mode)
            .HasColumnName("mode")
            .HasConversion(v => v.ToWire(), s => RetentionModeWire.FromWire(s)!.Value)
            .IsRequired()
            .HasMaxLength(16);
        builder.Ignore(x => x.IsEnabled);

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        // One rule per Workspace (its id is the Workspace id). The job scans the rows that have a window.
        builder.HasIndex(x => x.WorkspaceId).IsUnique().HasDatabaseName("ux_retention_policies_workspace");
        builder.HasIndex(x => x.RetentionDays)
            .HasFilter("retention_days IS NOT NULL")
            .HasDatabaseName("ix_retention_policies_enabled");
    }
}
