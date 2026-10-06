using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRGA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendInterestRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "MonthlyRate",
                table: "InterestRates",
                type: "decimal(18,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<decimal>(
                name: "DailyRate",
                table: "InterestRates",
                type: "decimal(18,8)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Norm",
                table: "InterestRates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // defaultValue tiene que ser 1 (InterestType.Resarcitorio), NO 0: el 0 no es un valor válido
            // del enum y dejaría la fila de tasa que ya existe con un tipo inexistente.
            migrationBuilder.AddColumn<int>(
                name: "TypeOfInterest",
                table: "InterestRates",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidTo",
                table: "InterestRates",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyRate",
                table: "InterestRates");

            migrationBuilder.DropColumn(
                name: "Norm",
                table: "InterestRates");

            migrationBuilder.DropColumn(
                name: "TypeOfInterest",
                table: "InterestRates");

            migrationBuilder.DropColumn(
                name: "ValidTo",
                table: "InterestRates");

            migrationBuilder.AlterColumn<decimal>(
                name: "MonthlyRate",
                table: "InterestRates",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)");
        }
    }
}
