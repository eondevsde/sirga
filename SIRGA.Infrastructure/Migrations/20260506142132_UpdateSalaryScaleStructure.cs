using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRGA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSalaryScaleStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "SalaryScales");

            migrationBuilder.DropColumn(
                name: "EffectiveEndDate",
                table: "SalaryScales");

            migrationBuilder.RenameColumn(
                name: "MinimumAmount",
                table: "SalaryScales",
                newName: "BasicAmount");

            migrationBuilder.RenameColumn(
                name: "MaximumAmount",
                table: "SalaryScales",
                newName: "AgreementNR2");

            migrationBuilder.RenameColumn(
                name: "EffectiveStartDate",
                table: "SalaryScales",
                newName: "Period");

            migrationBuilder.AddColumn<decimal>(
                name: "AgreementNR1",
                table: "SalaryScales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CategoryCode",
                table: "SalaryScales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "SalaryScales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UnionId",
                table: "SalaryScales",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalaryScales_UnionId",
                table: "SalaryScales",
                column: "UnionId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalaryScales_Unions_UnionId",
                table: "SalaryScales",
                column: "UnionId",
                principalTable: "Unions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalaryScales_Unions_UnionId",
                table: "SalaryScales");

            migrationBuilder.DropIndex(
                name: "IX_SalaryScales_UnionId",
                table: "SalaryScales");

            migrationBuilder.DropColumn(
                name: "AgreementNR1",
                table: "SalaryScales");

            migrationBuilder.DropColumn(
                name: "CategoryCode",
                table: "SalaryScales");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "SalaryScales");

            migrationBuilder.DropColumn(
                name: "UnionId",
                table: "SalaryScales");

            migrationBuilder.RenameColumn(
                name: "Period",
                table: "SalaryScales",
                newName: "EffectiveStartDate");

            migrationBuilder.RenameColumn(
                name: "BasicAmount",
                table: "SalaryScales",
                newName: "MinimumAmount");

            migrationBuilder.RenameColumn(
                name: "AgreementNR2",
                table: "SalaryScales",
                newName: "MaximumAmount");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "SalaryScales",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveEndDate",
                table: "SalaryScales",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
