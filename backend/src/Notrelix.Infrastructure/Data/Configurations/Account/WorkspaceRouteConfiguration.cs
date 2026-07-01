using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(x => new { x.AccountId, x.RouteSlug }).IsUnique().HasDatabaseName("idx_workspace_routes_account_slug");
    }
}
