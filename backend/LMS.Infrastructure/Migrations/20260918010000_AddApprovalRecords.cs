using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "approval_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    leave_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comp_off_request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    acted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reminder_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_records", x => x.id);

                    // FK to users (approver) — always enforced with RESTRICT to prevent orphan records
                    table.ForeignKey(
                        name: "fk_approval_records_approver_id",
                        column: x => x.approver_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Composite index: all approvals for a specific leave request at a given level
            migrationBuilder.CreateIndex(
                name: "idx_approval_records_leave_request_level",
                table: "approval_records",
                columns: new[] { "leave_request_id", "level" });

            // Composite index: approver workload and history queries
            migrationBuilder.CreateIndex(
                name: "idx_approval_records_approver_acted_at",
                table: "approval_records",
                columns: new[] { "approver_id", "acted_at" });

            // Index for comp-off approval lookups
            migrationBuilder.CreateIndex(
                name: "idx_approval_records_comp_off_request_id",
                table: "approval_records",
                column: "comp_off_request_id");

            // Partial index for pending approvals — used by Hangfire reminder job
            migrationBuilder.Sql(
                """
                CREATE INDEX idx_approval_records_pending
                    ON approval_records (approver_id, created_at)
                    WHERE acted_at IS NULL AND deleted_at IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "approval_records");
        }
    }
}
