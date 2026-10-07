using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRGA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRectificationVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // OJO: EF propone defaultValue: 0, y eso dejaría las 227.174 DDJJ que ya existen en versión 0.
            // La versión 1 es la original (el mismo criterio del sistema anterior, donde rect = 0 era la
            // original): corregido a mano.
            migrationBuilder.AddColumn<int>(
                name: "VersionNumber",
                table: "Declarations",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "Declarations");
        }
    }
}
