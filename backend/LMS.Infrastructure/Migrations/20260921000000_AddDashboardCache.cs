using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// F-11 Dashboards -- DB Layer.
    ///
    /// Dashboards are read-only aggregate views over existing data (LeaveRequests,
    /// LeaveBalances, CompOffCredits, ApprovalRecords, Employees). This migration
    /// adds the DashboardCaches table for storing computed dashboard snapshots,
    /// improving response times for expensive aggregation queries.
    /// </summary>
    public partial class AddDashboardCache : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dashboard_caches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    dashboard_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    filter_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    cache_data = table.Column<string>(type: "text", nullable: false),
                    computed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dashboard_caches", x => x.id);
                    table.CheckConstraint(
                        name: "chk_dashboard_caches_dashboard_type",
                        sql: "dashboard_type IN ('TeamLeave', 'Approvals', 'CompOff', 'Overview')");
                });

            // Composite index for fast lookup by type + filter key
            migrationBuilder.CreateIndex(
                name: "idx_dashboard_caches_type_filter",
                table: "dashboard_caches",
                columns: new[] { "dashboard_type", "filter_key" });

            // Index on expires_at for cache cleanup jobs
            migrationBuilder.CreateIndex(
                name: "idx_dashboard_caches_expires_at",
                table: "dashboard_caches",
                column: "expires_at");

            // Partial index for active (non-expired, non-deleted) cache entries
            migrationBuilder.Sql(
                """
                CREATE INDEX idx_dashboard_caches_active
                    ON dashboard_caches (dashboard_type, filter_key, expires_at)
                    WHERE deleted_at IS NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "dashboard_caches");
        }
    }
}
