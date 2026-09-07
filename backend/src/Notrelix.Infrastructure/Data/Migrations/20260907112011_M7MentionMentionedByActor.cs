#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class M7MentionMentionedByActor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // M2F: mention creation now captures the trusted mentioner actor.
            // The mention pipeline produced no rows before this change (no
            // producer existed), so the Guid.Empty default satisfies NOT NULL
            // for an empty table; new rows always carry a real actor guarded
            // by the Domain aggregate.
            migrationBuilder.AddColumn<Guid>(
                name: "mentioned_by_user_id",
                schema: "collab",
                table: "mentions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mentioned_by_user_id",
                schema: "collab",
                table: "mentions");
        }
    }
}
