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
            // Tokens issued before scopes existed are granted all seven scopes, so none is left without one; idempotent.
            // They still lose the contexts no scope maps (enforcement, not this step); see ApiTokenScopeMigrationSql.
            migrationBuilder.Sql(ApiTokenScopeMigrationSql.Backfill);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Forward-only data step: the scopes a token was granted stay.
        }
    }
}
