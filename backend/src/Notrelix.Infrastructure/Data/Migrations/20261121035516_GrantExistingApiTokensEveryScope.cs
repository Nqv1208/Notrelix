using Notrelix.Infrastructure.Data.Identity;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GrantExistingApiTokensEveryScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tokens issued before scopes existed are granted every scope, so none is left without one; idempotent.
            migrationBuilder.Sql(ApiTokenScopeMigrationSql.Backfill);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Forward-only data step: the scopes a token was granted stay.
        }
    }
}
