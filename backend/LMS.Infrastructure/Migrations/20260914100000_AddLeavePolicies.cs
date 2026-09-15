using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeavePolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // leave_types table
            migrationBuilder.CreateTable(
                name: "leave_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    annual_days = table.Column<int>(type: "integer", nullable: false),
                    requires_attachment = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    requires_hr_approval = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_types", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_leave_types_code",
                table: "leave_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_leave_types_deleted_at",
                table: "leave_types",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "idx_leave_types_is_active",
                table: "leave_types",
                column: "is_active");

            // leave_policies table
            migrationBuilder.CreateTable(
                name: "leave_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    applicable_to_role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    annual_allotment = table.Column<int>(type: "integer", nullable: false),
                    max_carry_forward = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max_consecutive_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    min_notice_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    accrual_monthly = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    accrual_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_policies", x => x.id);
                    table.ForeignKey(
                        name: "fk_leave_policies_leave_type_id",
                        column: x => x.leave_type_id,
                        principalTable: "leave_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_leave_policies_leave_type_id",
                table: "leave_policies",
                column: "leave_type_id");

            migrationBuilder.CreateIndex(
                name: "idx_leave_policies_is_active",
                table: "leave_policies",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "idx_leave_policies_effective_from",
                table: "leave_policies",
                column: "effective_from");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "leave_policies");
            migrationBuilder.DropTable(name: "leave_types");
        }
    }
}
