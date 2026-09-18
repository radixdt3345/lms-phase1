using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// F-15 Background Jobs (Hangfire) -- DB Layer.
    ///
    /// Hangfire's own tables (hangfire.job, hangfire.state, hangfire.counter,
    /// hangfire.hash, hangfire.list, hangfire.set, hangfire.server) are created
    /// automatically by UseHangfireStorage(connectionString) on application startup.
    /// No EF migration is required for those tables.
    ///
    /// This migration creates the application-level job_logs table for recording
    /// business-relevant outcomes of each background job run (audit trail, monitoring).
    /// </summary>
    public partial class AddJobLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    job_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    job_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Running"),
                    executed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    error = table.Column<string>(type: "text", nullable: true),
                    result_payload = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_logs", x => x.id);
                    table.CheckConstraint(
                        name: "chk_job_logs_status",
                        sql: "status IN ('Running', 'Succeeded', 'Failed')");
                });

            migrationBuilder.CreateIndex(
                name: "idx_job_logs_job_name",
                table: "job_logs",
                column: "job_name");

            migrationBuilder.CreateIndex(
                name: "idx_job_logs_executed_at",
                table: "job_logs",
                column: "executed_at");

            migrationBuilder.Sql(
                """
                CREATE INDEX idx_job_logs_failed
                    ON job_logs (job_name, executed_at)
                    WHERE status = 'Failed';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "job_logs");
        }
    }
}
