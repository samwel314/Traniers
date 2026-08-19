using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addTrainee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcademyTrainees_Academies_AcademyId",
                table: "AcademyTrainees");

            migrationBuilder.DropForeignKey(
                name: "FK_AcademyTrainees_Trainees_TraineeId",
                table: "AcademyTrainees");

            migrationBuilder.DropForeignKey(
                name: "FK_Trainees_Parents_ParentId",
                table: "Trainees");

            migrationBuilder.DropTable(
                name: "Parents");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Trainees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_AcademyTrainees_Academies_AcademyId",
                table: "AcademyTrainees",
                column: "AcademyId",
                principalTable: "Academies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AcademyTrainees_Trainees_TraineeId",
                table: "AcademyTrainees",
                column: "TraineeId",
                principalTable: "Trainees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Trainees_Trainees_ParentId",
                table: "Trainees",
                column: "ParentId",
                principalTable: "Trainees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcademyTrainees_Academies_AcademyId",
                table: "AcademyTrainees");

            migrationBuilder.DropForeignKey(
                name: "FK_AcademyTrainees_Trainees_TraineeId",
                table: "AcademyTrainees");

            migrationBuilder.DropForeignKey(
                name: "FK_Trainees_Trainees_ParentId",
                table: "Trainees");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Trainees");

            migrationBuilder.CreateTable(
                name: "Parents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Photo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Parents_PhoneNumber",
                table: "Parents",
                column: "PhoneNumber");

            migrationBuilder.AddForeignKey(
                name: "FK_AcademyTrainees_Academies_AcademyId",
                table: "AcademyTrainees",
                column: "AcademyId",
                principalTable: "Academies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AcademyTrainees_Trainees_TraineeId",
                table: "AcademyTrainees",
                column: "TraineeId",
                principalTable: "Trainees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Trainees_Parents_ParentId",
                table: "Trainees",
                column: "ParentId",
                principalTable: "Parents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
