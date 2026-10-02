using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRGA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyStudioHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyStudioHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    AccountingStudioId = table.Column<int>(type: "int", nullable: false),
                    UnionId = table.Column<int>(type: "int", nullable: true),
                    From = table.Column<DateTime>(type: "datetime2", nullable: false),
                    To = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AuthorizedByUsername = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyStudioHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyStudioHistories_AccountingStudios_AccountingStudioId",
                        column: x => x.AccountingStudioId,
                        principalTable: "AccountingStudios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyStudioHistories_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyStudioHistories_Unions_UnionId",
                        column: x => x.UnionId,
                        principalTable: "Unions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyStudioHistories_AccountingStudioId",
                table: "CompanyStudioHistories",
                column: "AccountingStudioId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyStudioHistories_CompanyId",
                table: "CompanyStudioHistories",
                column: "CompanyId",
                unique: true,
                filter: "[To] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyStudioHistories_UnionId",
                table: "CompanyStudioHistories",
                column: "UnionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyStudioHistories");
        }
    }
}
