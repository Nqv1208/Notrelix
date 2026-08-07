#nullable disable

namespace Notrelix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceIdempotencyRecordStatePayloadInvariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM ops.idempotency_records
                        WHERE NOT (
                            (
                              state = 'Processing'
                              AND result_json IS NULL
                              AND result_contract IS NULL
                              AND completed_at IS NULL
                            )
                            OR
                            (
                              state = 'Completed'
                              AND result_json IS NOT NULL
                              AND result_contract IS NOT NULL
                              AND completed_at IS NOT NULL
                            )
                        )
                    ) THEN
                        RAISE EXCEPTION
                            'Cannot enforce idempotency state/payload invariant: invalid rows exist in ops.idempotency_records';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "ck_idempotency_records_completed_result",
                schema: "ops",
                table: "idempotency_records");

            migrationBuilder.AddCheckConstraint(
                name: "ck_idempotency_records_completed_result",
                schema: "ops",
                table: "idempotency_records",
                sql: "(\n  state = 'Processing'\n  AND result_json IS NULL\n  AND result_contract IS NULL\n  AND completed_at IS NULL\n)\nOR\n(\n  state = 'Completed'\n  AND result_json IS NOT NULL\n  AND result_contract IS NOT NULL\n  AND completed_at IS NOT NULL\n)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_idempotency_records_completed_result",
                schema: "ops",
                table: "idempotency_records");

            migrationBuilder.AddCheckConstraint(
                name: "ck_idempotency_records_completed_result",
                schema: "ops",
                table: "idempotency_records",
                sql: "state <> 'Completed' OR (result_json IS NOT NULL AND result_contract IS NOT NULL AND completed_at IS NOT NULL)");
        }
    }
}
