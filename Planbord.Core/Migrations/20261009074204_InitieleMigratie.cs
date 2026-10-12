using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Planbord.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitieleMigratie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Medewerkers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Naam = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Telefoon = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Actief = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medewerkers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projecten",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Naam = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Adres = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Gearchiveerd = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projecten", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Afwezigheden",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MedewerkerId = table.Column<int>(type: "integer", nullable: false),
                    Van = table.Column<DateOnly>(type: "date", nullable: false),
                    Tot = table.Column<DateOnly>(type: "date", nullable: false),
                    Reden = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Afwezigheden", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Afwezigheden_Medewerkers_MedewerkerId",
                        column: x => x.MedewerkerId,
                        principalTable: "Medewerkers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    MedewerkerId = table.Column<int>(type: "integer", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Medewerkers_MedewerkerId",
                        column: x => x.MedewerkerId,
                        principalTable: "Medewerkers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Berichten",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MedewerkerId = table.Column<int>(type: "integer", nullable: false),
                    Datum = table.Column<DateOnly>(type: "date", nullable: false),
                    ExtraTekst = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    GezienOp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Berichten", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Berichten_Medewerkers_MedewerkerId",
                        column: x => x.MedewerkerId,
                        principalTable: "Medewerkers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dagrapporten",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MedewerkerId = table.Column<int>(type: "integer", nullable: false),
                    Datum = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Start = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Einde = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    PauzeMinuten = table.Column<int>(type: "integer", nullable: false),
                    IngediendOp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GeslotenOp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VrijgegevenVoorCorrectie = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dagrapporten", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dagrapporten_Medewerkers_MedewerkerId",
                        column: x => x.MedewerkerId,
                        principalTable: "Medewerkers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Planningen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MedewerkerId = table.Column<int>(type: "integer", nullable: false),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    Datum = table.Column<DateOnly>(type: "date", nullable: false),
                    ToegewezenOp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsInterventie = table.Column<bool>(type: "boolean", nullable: false),
                    Goedgekeurd = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planningen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Planningen_Medewerkers_MedewerkerId",
                        column: x => x.MedewerkerId,
                        principalTable: "Medewerkers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Planningen_Projecten_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projecten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Taken",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    Omschrijving = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Klaar = table.Column<bool>(type: "boolean", nullable: false),
                    KlaarOp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    KlaarDoor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Taken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Taken_Projecten_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projecten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DagrapportRegels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DagrapportId = table.Column<int>(type: "integer", nullable: false),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Omschrijving = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Minuten = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DagrapportRegels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DagrapportRegels_Dagrapporten_DagrapportId",
                        column: x => x.DagrapportId,
                        principalTable: "Dagrapporten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DagrapportRegels_Projecten_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projecten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Afwezigheden_MedewerkerId",
                table: "Afwezigheden",
                column: "MedewerkerId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_MedewerkerId",
                table: "AspNetUsers",
                column: "MedewerkerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Berichten_MedewerkerId_Datum",
                table: "Berichten",
                columns: new[] { "MedewerkerId", "Datum" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dagrapporten_MedewerkerId_Datum",
                table: "Dagrapporten",
                columns: new[] { "MedewerkerId", "Datum" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DagrapportRegels_DagrapportId",
                table: "DagrapportRegels",
                column: "DagrapportId");

            migrationBuilder.CreateIndex(
                name: "IX_DagrapportRegels_ProjectId",
                table: "DagrapportRegels",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Planningen_Datum_MedewerkerId",
                table: "Planningen",
                columns: new[] { "Datum", "MedewerkerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Planningen_MedewerkerId",
                table: "Planningen",
                column: "MedewerkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Planningen_ProjectId",
                table: "Planningen",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Taken_ProjectId",
                table: "Taken",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Afwezigheden");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Berichten");

            migrationBuilder.DropTable(
                name: "DagrapportRegels");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "Planningen");

            migrationBuilder.DropTable(
                name: "Taken");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Dagrapporten");

            migrationBuilder.DropTable(
                name: "Projecten");

            migrationBuilder.DropTable(
                name: "Medewerkers");
        }
    }
}
