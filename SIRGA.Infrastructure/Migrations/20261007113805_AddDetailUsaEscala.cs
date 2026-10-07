using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRGA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDetailUsaEscala : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // OJO: EF propone defaultValue: false, que es el valor INSEGURO. Marcaria todos los detalles que
            // ya existen como "importe cargado a mano", y entonces pre-cargar una rectificativa copiaria
            // como tipeados unos importes que en realidad salieron de la escala (congelando minimos viejos).
            // Para las filas que ya existen, el valor seguro es TRUE (salio de la escala): asi no se copia.
            // Corregido a mano.
            migrationBuilder.AddColumn<bool>(
                name: "UsaEscala",
                table: "DeclarationDetails",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsaEscala",
                table: "DeclarationDetails");
        }
    }
}
