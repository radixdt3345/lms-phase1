using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// F-12 Reports and CSV Export -- DB Layer.
    ///
    /// Creates the report_jobs table for tracking asynchronous report generation
    /// requests (LeaveSummary, CompOffSummary, ApprovalHistory). Reports are
    /// generated as background jobs; this table serves as the job queue and
    /// result store.
    /// </summary>
    public partial class AddReportJobs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "report_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    report_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    requested_by_user_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    filter_json = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    output_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_jobs", x => x.id);
                    table.CheckConstraint(
                        name: "chk_report_jobs_report_type",
                        sql: "report_type IN ('LeaveSummary', 'CompOffSummary', 'ApprovalHistory')");
                    table.CheckConstraint(
                        name: "chk_report_jobs_status",
                        sql: "status IN ('Pending', 'Processing', 'Completed', 'Failed')");
                });

            // Index on status for queue polling (workers pick up Pending jobs)
            migrationBuilder.CreateIndex(
                name: "idx_report_jobs_status",
                table: "report_jobs",
                column: "status");

            // Index on requested_by_user_id for user-facing job history
            migrationBuilder.CreateIndex(
                name: "idx_report_jobs_requested_by_user_id",
                table: "report_jobs",
                column: "requested_by_user_id");

            // Partial index for pending jobs (fast queue drain)
            migrationBuilder.Sql(
                """
                CREATE INDEX idx_report_jobs_pending
                    ON report_jobs (requested_at)
                    WHERE status = 'Pending' AND deleted_at IS NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "report_jobs");
        }
    }
}
