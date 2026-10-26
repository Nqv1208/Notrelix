using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Additive: a comment may point at a quoted range of one block of its page. Every column is nullable, so
    /// existing comments keep an empty anchor and old readers ignore the new columns (expand step; nothing to
    /// backfill or contract). The target model is the model snapshot at this migration.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261026120000_AddCommentTextAnchor")]
    public partial class AddCommentTextAnchor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "anchor_block_id",
                schema: "collab",
                table: "comments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "anchor_end",
                schema: "collab",
                table: "comments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "anchor_quote",
                schema: "collab",
                table: "comments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "anchor_start",
                schema: "collab",
                table: "comments",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "anchor_block_id", schema: "collab", table: "comments");
            migrationBuilder.DropColumn(name: "anchor_end", schema: "collab", table: "comments");
            migrationBuilder.DropColumn(name: "anchor_quote", schema: "collab", table: "comments");
            migrationBuilder.DropColumn(name: "anchor_start", schema: "collab", table: "comments");
        }
    }
}
