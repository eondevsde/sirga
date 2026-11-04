using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRGA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPropuestaEscalaAlDetalle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Difference",
                table: "DeclarationDetails",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EnteredSalary",
                table: "DeclarationDetails",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ScaleSalary",
                table: "DeclarationDetails",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Difference",
                table: "DeclarationDetails");

            migrationBuilder.DropColumn(
                name: "EnteredSalary",
                table: "DeclarationDetails");

            migrationBuilder.DropColumn(
                name: "ScaleSalary",
                table: "DeclarationDetails");
        }
    }
}
