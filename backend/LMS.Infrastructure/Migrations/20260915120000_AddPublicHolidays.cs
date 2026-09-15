using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicHolidays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "public_holidays",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    is_optional = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_public_holidays", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_public_holidays_country_code",
                table: "public_holidays",
                column: "country_code");

            migrationBuilder.CreateIndex(
                name: "idx_public_holidays_year_date",
                table: "public_holidays",
                columns: new[] { "year", "date" });

            migrationBuilder.CreateIndex(
                name: "idx_public_holidays_year_is_active",
                table: "public_holidays",
                columns: new[] { "year", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "public_holidays");
        }
    }
}
