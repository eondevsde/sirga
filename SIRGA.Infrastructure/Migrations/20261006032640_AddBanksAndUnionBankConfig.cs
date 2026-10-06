using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRGA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBanksAndUnionBankConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Banks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Branch = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnionBankConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnionId = table.Column<int>(type: "int", nullable: false),
                    BankId = table.Column<int>(type: "int", nullable: false),
                    ClientNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EntityCode = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    DesignFormat = table.Column<int>(type: "int", nullable: false),
                    FirstDueDay = table.Column<int>(type: "int", nullable: false),
                    DaysToSecondDue = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnionBankConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnionBankConfigs_Banks_BankId",
                        column: x => x.BankId,
                        principalTable: "Banks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnionBankConfigs_Unions_UnionId",
                        column: x => x.UnionId,
                        principalTable: "Unions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnionBankConfigs_BankId",
                table: "UnionBankConfigs",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_UnionBankConfigs_UnionId",
                table: "UnionBankConfigs",
                column: "UnionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnionBankConfigs");

            migrationBuilder.DropTable(
                name: "Banks");
        }
    }
}
