using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planbord.Core.Migrations
{
    /// <inheritdoc />
    public partial class ProjectPeriode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "Einddatum",
                table: "Projecten",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "Startdatum",
                table: "Projecten",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Einddatum",
                table: "Projecten");

            migrationBuilder.DropColumn(
                name: "Startdatum",
                table: "Projecten");
        }
    }
}
