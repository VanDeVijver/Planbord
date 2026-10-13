using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planbord.Core.Migrations
{
    /// <inheritdoc />
    public partial class Projectleider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectleiderId",
                table: "Projecten",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsProjectleider",
                table: "Medewerkers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Existing projects need a leader before the foreign key exists: use the first employee
            // (marked as project leader), or a placeholder leader when there are projects but no employees.
            migrationBuilder.Sql("""
                INSERT INTO "Medewerkers" ("Naam", "Actief", "IsProjectleider")
                SELECT 'Projectleider (onbekend)', TRUE, TRUE
                WHERE EXISTS (SELECT 1 FROM "Projecten") AND NOT EXISTS (SELECT 1 FROM "Medewerkers");
                UPDATE "Projecten" SET "ProjectleiderId" = (SELECT MIN("Id") FROM "Medewerkers") WHERE "ProjectleiderId" = 0;
                UPDATE "Medewerkers" SET "IsProjectleider" = TRUE WHERE "Id" IN (SELECT "ProjectleiderId" FROM "Projecten");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Projecten_ProjectleiderId",
                table: "Projecten",
                column: "ProjectleiderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projecten_Medewerkers_ProjectleiderId",
                table: "Projecten",
                column: "ProjectleiderId",
                principalTable: "Medewerkers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projecten_Medewerkers_ProjectleiderId",
                table: "Projecten");

            migrationBuilder.DropIndex(
                name: "IX_Projecten_ProjectleiderId",
                table: "Projecten");

            migrationBuilder.DropColumn(
                name: "ProjectleiderId",
                table: "Projecten");

            migrationBuilder.DropColumn(
                name: "IsProjectleider",
                table: "Medewerkers");
        }
    }
}
