using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // leave_request_attachments — file metadata (created before leave_requests to satisfy FK below)
            migrationBuilder.CreateTable(
                name: "leave_request_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    leave_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    storage_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_request_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_leave_request_attachments_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    // leave_request_id FK added below after leave_requests is created
                });

            // leave_requests — central leave lifecycle table
            migrationBuilder.CreateTable(
                name: "leave_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_half_day = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    half_day_period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Draft"),
                    calculated_days = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false, defaultValue: 0m),
                    attachment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_leave_requests_employee_id",
                        column: x => x.employee_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_leave_requests_leave_type_id",
                        column: x => x.leave_type_id,
                        principalTable: "leave_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_leave_requests_attachment_id",
                        column: x => x.attachment_id,
                        principalTable: "leave_request_attachments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Add leave_request_id FK on attachments now that leave_requests exists
            migrationBuilder.AddForeignKey(
                name: "fk_leave_request_attachments_leave_request_id",
                table: "leave_request_attachments",
                column: "leave_request_id",
                principalTable: "leave_requests",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            // Add FK from comp_off_credits.leave_request_id to leave_requests now that the table exists
            migrationBuilder.AddForeignKey(
                name: "fk_comp_off_credits_leave_request_id",
                table: "comp_off_credits",
                column: "leave_request_id",
                principalTable: "leave_requests",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // Indexes for leave_requests
            migrationBuilder.CreateIndex(
                name: "idx_leave_requests_employee_id",
                table: "leave_requests",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "idx_leave_requests_status",
                table: "leave_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_leave_requests_start_date",
                table: "leave_requests",
                column: "start_date");

            migrationBuilder.CreateIndex(
                name: "idx_leave_requests_employee_status_startdate",
                table: "leave_requests",
                columns: new[] { "employee_id", "status", "start_date" });

            migrationBuilder.CreateIndex(
                name: "idx_leave_requests_leave_type_id",
                table: "leave_requests",
                column: "leave_type_id");

            // Indexes for leave_request_attachments
            migrationBuilder.CreateIndex(
                name: "idx_leave_request_attachments_leave_request_id",
                table: "leave_request_attachments",
                column: "leave_request_id");

            migrationBuilder.CreateIndex(
                name: "idx_leave_request_attachments_uploaded_by",
                table: "leave_request_attachments",
                column: "uploaded_by");

            // Index on comp_off_credits.leave_request_id now that FK is established
            migrationBuilder.CreateIndex(
                name: "idx_comp_off_credits_leave_request_id",
                table: "comp_off_credits",
                column: "leave_request_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove FK from comp_off_credits before dropping leave_requests
            migrationBuilder.DropForeignKey(
                name: "fk_comp_off_credits_leave_request_id",
                table: "comp_off_credits");

            migrationBuilder.DropIndex(
                name: "idx_comp_off_credits_leave_request_id",
                table: "comp_off_credits");

            migrationBuilder.DropTable(name: "leave_requests");
            migrationBuilder.DropTable(name: "leave_request_attachments");
        }
    }
}
