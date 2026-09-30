using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRGA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Declarations_CompanyId",
                table: "Declarations");

            migrationBuilder.DropIndex(
                name: "IX_Declarations_UnionId",
                table: "Declarations");

            migrationBuilder.CreateIndex(
                name: "IX_Declarations_CompanyId_Period",
                table: "Declarations",
                columns: new[] { "CompanyId", "Period" });

            migrationBuilder.CreateIndex(
                name: "IX_Declarations_Period",
                table: "Declarations",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_Declarations_Status_SubmissionDate",
                table: "Declarations",
                columns: new[] { "Status", "SubmissionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Declarations_UnionId_SubmissionDate",
                table: "Declarations",
                columns: new[] { "UnionId", "SubmissionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BalanceHistories_CompanyId",
                table: "BalanceHistories",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Declarations_CompanyId_Period",
                table: "Declarations");

            migrationBuilder.DropIndex(
                name: "IX_Declarations_Period",
                table: "Declarations");

            migrationBuilder.DropIndex(
                name: "IX_Declarations_Status_SubmissionDate",
                table: "Declarations");

            migrationBuilder.DropIndex(
                name: "IX_Declarations_UnionId_SubmissionDate",
                table: "Declarations");

            migrationBuilder.DropIndex(
                name: "IX_BalanceHistories_CompanyId",
                table: "BalanceHistories");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs");

            migrationBuilder.CreateIndex(
                name: "IX_Declarations_CompanyId",
                table: "Declarations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Declarations_UnionId",
                table: "Declarations",
                column: "UnionId");
        }
    }
}
