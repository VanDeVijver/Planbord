using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Planbord.Core.Migrations
{
    /// <inheritdoc />
    public partial class CategorieAandelen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategorieAandelen",
                columns: table => new
                {
                    Categorie = table.Column<int>(type: "integer", nullable: false),
                    Procent = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategorieAandelen", x => x.Categorie);
                });

            migrationBuilder.InsertData(
                table: "CategorieAandelen",
                columns: new[] { "Categorie", "Procent" },
                values: new object[,]
                {
                    { 1, 35 },
                    { 2, 25 },
                    { 3, 20 },
                    { 4, 10 },
                    { 5, 10 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategorieAandelen");
        }
    }
}
