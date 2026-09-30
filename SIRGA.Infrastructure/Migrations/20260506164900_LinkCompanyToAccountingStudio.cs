using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRGA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkCompanyToAccountingStudio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountingStudioId",
                table: "Companies",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_AccountingStudioId",
                table: "Companies",
                column: "AccountingStudioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_AccountingStudios_AccountingStudioId",
                table: "Companies",
                column: "AccountingStudioId",
                principalTable: "AccountingStudios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_AccountingStudios_AccountingStudioId",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_AccountingStudioId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "AccountingStudioId",
                table: "Companies");
        }
    }
}
