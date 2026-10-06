using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRGA.Infrastructure.Migrations
{
    /// <summary>
    /// Las categorías y las escalas pasan a colgar de la RAMA de actividad (el convenio) y no del
    /// sindicato: las escalas y las paritarias son nacionales por rama, así que varios sindicatos de la
    /// misma rama comparten las mismas categorías y las mismas escalas.
    ///
    /// OJO: EF generó esto como un **RenameColumn** de `UnionId` a `RamaId`, y eso está MAL: renombrar
    /// conserva los valores viejos (19 = el id del sindicato) como si fueran ids de rama, y el FK contra
    /// `Ramas` falla porque la rama de Comercio es la 1. Hay que DERIVAR el valor de la rama del
    /// sindicato, no renombrar.
    /// </summary>
    public partial class MoveScalesAndCategoriesToRama : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------------------------------------------------------------------------------
            // RESPALDO: se están por eliminar dos columnas (UnionId de las dos tablas). El vínculo se
            // puede reconstruir desde la rama, pero se guarda igual: no se borra nada sin respaldo.
            // ---------------------------------------------------------------------------------
            migrationBuilder.Sql(@"
                IF OBJECT_ID('dbo._BackupScalesCategoriesUnionId') IS NULL
                BEGIN
                    SELECT 'EmployeeCategories' AS Tabla, Id AS RegistroId, UnionId
                    INTO dbo._BackupScalesCategoriesUnionId
                    FROM EmployeeCategories
                    UNION ALL
                    SELECT 'SalaryScales', Id, UnionId FROM SalaryScales;
                END");

            // ---------------------------------------------------------------------------------
            // EmployeeCategories: UnionId -> RamaId
            // ---------------------------------------------------------------------------------
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeCategories_Unions_UnionId",
                table: "EmployeeCategories");

            // La columna nueva entra NULLABLE: los valores se derivan y recién después pasa a NOT NULL.
            // Con el defaultValue 0 que genera EF, el FK fallaría (no existe una rama con id 0).
            migrationBuilder.AddColumn<int>(
                name: "RamaId",
                table: "EmployeeCategories",
                type: "int",
                nullable: true);

            // La rama del sindicato al que pertenecía la categoría.
            migrationBuilder.Sql(@"
                UPDATE c
                SET c.RamaId = u.RamaId
                FROM EmployeeCategories c
                INNER JOIN Unions u ON u.Id = c.UnionId
                WHERE u.RamaId IS NOT NULL;");

            // Si quedara alguna sin rama, el ALTER de abajo fallaría con un error genérico. Se corta acá
            // con un mensaje que dice qué hacer.
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM EmployeeCategories WHERE RamaId IS NULL)
                    THROW 50000, 'Hay categorias cuyo sindicato no tiene rama asignada. Asignala antes de migrar (script_sirga_db_ramas_vincular_sindicatos.sql).', 1;");

            migrationBuilder.AlterColumn<int>(
                name: "RamaId",
                table: "EmployeeCategories",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_EmployeeCategories_UnionId",
                table: "EmployeeCategories");

            migrationBuilder.DropColumn(
                name: "UnionId",
                table: "EmployeeCategories");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCategories_RamaId",
                table: "EmployeeCategories",
                column: "RamaId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeCategories_Ramas_RamaId",
                table: "EmployeeCategories",
                column: "RamaId",
                principalTable: "Ramas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ---------------------------------------------------------------------------------
            // SalaryScales: se va UnionId (la rama se llega por la categoría de la escala)
            // ---------------------------------------------------------------------------------
            migrationBuilder.DropForeignKey(
                name: "FK_SalaryScales_Unions_UnionId",
                table: "SalaryScales");

            migrationBuilder.DropIndex(
                name: "IX_SalaryScales_UnionId",
                table: "SalaryScales");

            migrationBuilder.DropColumn(
                name: "UnionId",
                table: "SalaryScales");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Se vuelve al modelo por sindicato. OJO: los valores de UnionId NO se pueden recuperar
            // exactamente (varios sindicatos comparten una rama), así que se rellena con el sindicato de
            // menor id que tenga esa rama. Alcanza para que el esquema vuelva atrás.
            migrationBuilder.AddColumn<int>(
                name: "UnionId",
                table: "SalaryScales",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE s
                SET s.UnionId = u.Id
                FROM SalaryScales s
                INNER JOIN EmployeeCategories c ON c.Id = s.EmployeeCategoryId
                CROSS APPLY (SELECT TOP 1 Id FROM Unions WHERE RamaId = c.RamaId ORDER BY Id) u;");

            migrationBuilder.AlterColumn<int>(
                name: "UnionId",
                table: "SalaryScales",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalaryScales_UnionId",
                table: "SalaryScales",
                column: "UnionId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalaryScales_Unions_UnionId",
                table: "SalaryScales",
                column: "UnionId",
                principalTable: "Unions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeCategories_Ramas_RamaId",
                table: "EmployeeCategories");

            migrationBuilder.AddColumn<int>(
                name: "UnionId",
                table: "EmployeeCategories",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE c
                SET c.UnionId = u.Id
                FROM EmployeeCategories c
                CROSS APPLY (SELECT TOP 1 Id FROM Unions WHERE RamaId = c.RamaId ORDER BY Id) u;");

            migrationBuilder.AlterColumn<int>(
                name: "UnionId",
                table: "EmployeeCategories",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_EmployeeCategories_RamaId",
                table: "EmployeeCategories");

            migrationBuilder.DropColumn(
                name: "RamaId",
                table: "EmployeeCategories");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCategories_UnionId",
                table: "EmployeeCategories",
                column: "UnionId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeCategories_Unions_UnionId",
                table: "EmployeeCategories",
                column: "UnionId",
                principalTable: "Unions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
