using Notrelix.Domain.Accounts.WorkspaceRoutes;

namespace Notrelix.Infrastructure.Data.Configurations.Account;

public class WorkspaceRouteConfiguration : IEntityTypeConfiguration<WorkspaceRoute>
{
    public void Configure(EntityTypeBuilder<WorkspaceRoute> builder)
    {
        builder.ToTable("workspace_routes", DbSchemas.Account);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.RouteSlug).HasColumnName("route_slug").IsRequired().HasMaxLength(128);
        builder.Property(x => x.IsDefault).HasColumnName("is_default");

        // AggregateRoot
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();

        // AuditableEntity
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        // SoftDeletableEntity
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason").HasMaxLength(500);
        builder.Property(x => x.RestoredAt).HasColumnName("restored_at");
        builder.Property(x => x.RestoredBy).HasColumnName("restored_by");

        builder.HasIndex(x => new { x.AccountId, x.RouteSlug }).IsUnique().HasDatabaseName("idx_workspace_routes_account_slug");
        builder.HasIndex(x => x.IsDeleted).HasDatabaseName("idx_workspace_routes_is_deleted");
    }
}