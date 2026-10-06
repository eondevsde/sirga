using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRGA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InterestRateSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InterestRates_EffectiveDate_TypeOfInterest",
                table: "InterestRates");

            // defaultValue TIENE que ser true: con el false que genera EF, las 75 tasas ya cargadas
            // quedarian DESACTIVADAS y la escala quedaria vacia (y la boleta volveria al 3% por defecto).
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "InterestRates",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterestRates_EffectiveDate_TypeOfInterest",
                table: "InterestRates",
                columns: new[] { "EffectiveDate", "TypeOfInterest" },
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InterestRates_EffectiveDate_TypeOfInterest",
                table: "InterestRates");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "InterestRates");

            migrationBuilder.CreateIndex(
                name: "IX_InterestRates_EffectiveDate_TypeOfInterest",
                table: "InterestRates",
                columns: new[] { "EffectiveDate", "TypeOfInterest" },
                unique: true);
        }
    }
}
