using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Notrelix.Domain.Workspaces.Invitations;

namespace Notrelix.Infrastructure.Data.Configurations.Workspaces;

public class WorkspaceInvitationConfiguration : IEntityTypeConfiguration<WorkspaceInvitation>
{
    private static readonly JsonSerializerOptions EntryJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public void Configure(EntityTypeBuilder<WorkspaceInvitation> builder)
    {
        builder.ToTable("workspace_invitations", DbSchemas.Workspace, t =>
        {
            // GOV-030 defence in depth: a Guest resource entry never exceeds Editor,
            // and the three resource columns are set together or not at all.
            t.HasCheckConstraint(
                "ck_workspace_invitations_resource_grant",
                "(resource_kind IS NULL AND resource_id IS NULL AND resource_level IS NULL) OR " +
                "(resource_kind IN ('work-management.board', 'documents.page') AND resource_id IS NOT NULL " +
                "AND resource_level IN ('Viewer', 'Commenter', 'Editor') AND role = 'Guest')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").IsRequired().HasMaxLength(256);
        builder.Property(x => x.Role).HasColumnName("role").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(x => x.InvitedBy).HasColumnName("invited_by").IsRequired();
        builder.Property(x => x.HashVersion).HasColumnName("hash_version").IsRequired().HasDefaultValue(1);
        builder.Property(x => x.TokenGeneration).HasColumnName("token_generation").IsRequired().HasDefaultValue(1);

        builder.OwnsOne(x => x.Token, token =>
        {
            token.Property(t => t.Value)
                .HasColumnName("token_hash")
                .HasMaxLength(InvitationTokenHash.HashLength)
                .HasColumnType($"character varying({InvitationTokenHash.HashLength})")
                .IsRequired();
        });

        // WSP-026: initial Team/Space entries applied on acceptance (jsonb, owned by
        // the invitation, never queried by content).
        var entriesComparer = new ValueComparer<IReadOnlyList<WorkspaceInvitationInitialEntry>>(
            (a, b) => JsonSerializer.Serialize(a, EntryJson) == JsonSerializer.Serialize(b, EntryJson),
            v => JsonSerializer.Serialize(v, EntryJson).GetHashCode(),
            v => (IReadOnlyList<WorkspaceInvitationInitialEntry>)v.ToArray());
        builder.Property(x => x.InitialEntries)
            .HasColumnName("initial_entries")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, EntryJson),
                v => (IReadOnlyList<WorkspaceInvitationInitialEntry>)(JsonSerializer.Deserialize<List<WorkspaceInvitationInitialEntry>>(v, EntryJson) ?? new List<WorkspaceInvitationInitialEntry>()),
                entriesComparer)
            .HasDefaultValueSql("'[]'::jsonb")
            .IsRequired();

        // WSP-026 / GOV-030: the single explicit resource entry of a resource invitation.
        builder.OwnsOne(x => x.ResourceGrant, grant =>
        {
            grant.Property(g => g.ResourceKind).HasColumnName("resource_kind").HasMaxLength(128);
            grant.Property(g => g.ResourceId).HasColumnName("resource_id");
            grant.Property(g => g.Level).HasColumnName("resource_level").HasMaxLength(32);
        });
        builder.Navigation(x => x.ResourceGrant);

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_workspace_invitations_workspace_id");
        builder.HasIndex(x => x.Email).HasDatabaseName("idx_workspace_invitations_email");
        // Token is owned into the same table; its unique index is maintained by
        // the migration SQL because EF cannot express an index over an owned
        // property with a simple member-access expression.
    }
}
