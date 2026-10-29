#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Additive: a rejected form submission keeps the reason the manager gave (max 500 chars). Nullable, so existing
    /// rows need no backfill (expand step; nothing to contract). The target model is the model snapshot at this migration.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261029100000_AddFormSubmissionRejectionReason")]
    public partial class AddFormSubmissionRejectionReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "reason",
                schema: "work",
                table: "form_submissions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "reason", schema: "work", table: "form_submissions");
        }
    }
}
