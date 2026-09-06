#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddResourcePermissionActiveUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "uq_resource_permissions_active_subject",
                schema: "governance",
                table: "resource_permissions",
                columns: new[] { "workspace_id", "resource_type", "resource_id", "subject_type", "subject_id" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_resource_permissions_active_subject",
                schema: "governance",
                table: "resource_permissions");
        }
    }
}