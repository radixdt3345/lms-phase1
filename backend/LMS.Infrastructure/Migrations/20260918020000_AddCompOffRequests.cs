using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompOffRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // comp_off_requests — employee requests to earn compensatory leave for working on
            // a public holiday or weekend. Approved requests trigger a CompOffCredit record (F-05).
            migrationBuilder.CreateTable(
                name: "comp_off_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date_worked = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    is_half_day = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    calculated_credit = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: false, defaultValue: 0m),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    approver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comp_off_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_comp_off_requests_employee_id",
                        column: x => x.employee_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_comp_off_requests_approver_id",
                        column: x => x.approver_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_comp_off_requests_employee_id",
                table: "comp_off_requests",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "idx_comp_off_requests_status",
                table: "comp_off_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_comp_off_requests_approver_id",
                table: "comp_off_requests",
                column: "approver_id");

            migrationBuilder.CreateIndex(
                name: "idx_comp_off_requests_date_worked",
                table: "comp_off_requests",
                column: "date_worked");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "comp_off_requests");
        }
    }
}
