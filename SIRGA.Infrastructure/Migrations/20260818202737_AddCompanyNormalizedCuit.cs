using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRGA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyNormalizedCuit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedCuit",
                table: "Companies",
                type: "nvarchar(450)",
                nullable: false,
                computedColumnSql: "ISNULL(REPLACE(REPLACE([Cuit], '-', ''), ' ', ''), '')",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_NormalizedCuit",
                table: "Companies",
                column: "NormalizedCuit",
                unique: true,
                filter: "[ParentCompanyId] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Companies_NormalizedCuit",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "NormalizedCuit",
                table: "Companies");
        }
    }
}
