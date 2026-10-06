using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRGA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJornadaToAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue tiene que ser 1 (JornadaType.Completa), NO 0. El 0 no es un valor válido
            // del enum: dejaría las 36.019 asignaciones existentes con un valor inexistente, que en
            // pantalla no coincide con ninguna opción. El sistema anterior no tenía el dato de
            // jornada, así que darlo por parcial (2) sería inventar una quita de haberes.
            migrationBuilder.AddColumn<int>(
                name: "Jornada",
                table: "Assignments",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Jornada",
                table: "Assignments");
        }
    }
}
