using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRGA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInterestRateUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_InterestRates_EffectiveDate_TypeOfInterest",
                table: "InterestRates",
                columns: new[] { "EffectiveDate", "TypeOfInterest" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InterestRates_EffectiveDate_TypeOfInterest",
                table: "InterestRates");
        }
    }
}
