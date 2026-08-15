using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostCurrency",
                schema: "inventory",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "SellingCurrency",
                schema: "inventory",
                table: "Products",
                newName: "Currency");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Currency",
                schema: "inventory",
                table: "Products",
                newName: "SellingCurrency");

            migrationBuilder.AddColumn<string>(
                name: "CostCurrency",
                schema: "inventory",
                table: "Products",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");
        }
    }
}
