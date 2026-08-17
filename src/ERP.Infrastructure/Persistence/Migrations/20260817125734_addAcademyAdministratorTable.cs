using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addAcademyAdministratorTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademyAdministrators",
                columns: table => new
                {
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdministratorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademyAdministrators", x => new { x.AcademyId, x.AdministratorId });
                    table.ForeignKey(
                        name: "FK_AcademyAdministrators_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AcademyAdministrators_Users_AdministratorId",
                        column: x => x.AdministratorId,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademyAdministrators_AdministratorId",
                table: "AcademyAdministrators",
                column: "AdministratorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademyAdministrators");
        }
    }
}
