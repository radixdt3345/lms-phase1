using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveBalances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // leave_balances — tracks each employee's balance per leave type per year
            migrationBuilder.CreateTable(
                name: "leave_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    total_days = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false, defaultValue: 0m),
                    used_days = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false, defaultValue: 0m),
                    pending_days = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false, defaultValue: 0m),
                    adjusted_days = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false, defaultValue: 0m),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_balances", x => x.id);
                    table.ForeignKey(
                        name: "fk_leave_balances_employee_id",
                        column: x => x.employee_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_leave_balances_leave_type_id",
                        column: x => x.leave_type_id,
                        principalTable: "leave_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Unique balance per employee per leave type per year (one row per combination)
            migrationBuilder.CreateIndex(
                name: "idx_leave_balances_employee_leavetype_year",
                table: "leave_balances",
                columns: new[] { "employee_id", "leave_type_id", "year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_leave_balances_employee_id",
                table: "leave_balances",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "idx_leave_balances_leave_type_id",
                table: "leave_balances",
                column: "leave_type_id");

            // comp_off_credits — individual comp-off credits with 30-day expiry
            // leave_request_id is stored as a nullable UUID; the FK to leave_requests is added
            // in the F-06 migration (20260918010000_AddLeaveRequests) once that table exists.
            migrationBuilder.CreateTable(
                name: "comp_off_credits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    earned_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    days = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    leave_request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comp_off_credits", x => x.id);
                    table.ForeignKey(
                        name: "fk_comp_off_credits_employee_id",
                        column: x => x.employee_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_comp_off_credits_employee_id",
                table: "comp_off_credits",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "idx_comp_off_credits_status",
                table: "comp_off_credits",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_comp_off_credits_expiry_date",
                table: "comp_off_credits",
                column: "expiry_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "comp_off_credits");
            migrationBuilder.DropTable(name: "leave_balances");
        }
    }
}
