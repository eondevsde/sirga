using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRGA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkUnionsToRama : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RamaId",
                table: "Unions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Unions_RamaId",
                table: "Unions",
                column: "RamaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Unions_Ramas_RamaId",
                table: "Unions",
                column: "RamaId",
                principalTable: "Ramas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Unions_Ramas_RamaId",
                table: "Unions");

            migrationBuilder.DropIndex(
                name: "IX_Unions_RamaId",
                table: "Unions");

            migrationBuilder.DropColumn(
                name: "RamaId",
                table: "Unions");
        }
    }
}
