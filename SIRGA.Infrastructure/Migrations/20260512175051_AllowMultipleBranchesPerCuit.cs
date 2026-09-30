using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRGA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleBranchesPerCuit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Companies_Cuit",
                table: "Companies");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Cuit_BranchName",
                table: "Companies",
                columns: new[] { "Cuit", "BranchName" },
                unique: true,
                filter: "[BranchName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Companies_Cuit_BranchName",
                table: "Companies");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Cuit",
                table: "Companies",
                column: "Cuit",
                unique: true);
        }
    }
}
