using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRGA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentActiveFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeCategories_Unions_UnionId1",
                table: "EmployeeCategories");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeCategories_UnionId1",
                table: "EmployeeCategories");

            migrationBuilder.DropColumn(
                name: "UnionId1",
                table: "EmployeeCategories");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Assignments",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TerminationDate",
                table: "Assignments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "TerminationDate",
                table: "Assignments");

            migrationBuilder.AddColumn<int>(
                name: "UnionId1",
                table: "EmployeeCategories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCategories_UnionId1",
                table: "EmployeeCategories",
                column: "UnionId1");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeCategories_Unions_UnionId1",
                table: "EmployeeCategories",
                column: "UnionId1",
                principalTable: "Unions",
                principalColumn: "Id");
        }
    }
}
