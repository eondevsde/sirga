using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRGA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStructureForHito3FinalV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalaryScales_Unions_UnionId",
                table: "SalaryScales");

            migrationBuilder.DropColumn(
                name: "CategoryCode",
                table: "SalaryScales");

            migrationBuilder.DropColumn(
                name: "BusinessName",
                table: "AssociationRequests");

            migrationBuilder.DropColumn(
                name: "CompanyCuit",
                table: "AssociationRequests");

            migrationBuilder.RenameColumn(
                name: "ResolutionDate",
                table: "AssociationRequests",
                newName: "ProcessedAt");

            migrationBuilder.RenameColumn(
                name: "RequestDate",
                table: "AssociationRequests",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Observation",
                table: "AssociationRequests",
                newName: "AdminComment");

            migrationBuilder.AddColumn<decimal>(
                name: "LawContributionPercentage",
                table: "Unions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnionMemberPercentage",
                table: "Unions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "UnionId",
                table: "SalaryScales",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AntiguedadPercentage",
                table: "SalaryScales",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "EmployeeCategoryId",
                table: "SalaryScales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "AssociationRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TargetUnionId",
                table: "AssociationRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EmployeeCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnionId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UnionId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeCategories_Unions_UnionId",
                        column: x => x.UnionId,
                        principalTable: "Unions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeCategories_Unions_UnionId1",
                        column: x => x.UnionId1,
                        principalTable: "Unions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalaryScales_EmployeeCategoryId",
                table: "SalaryScales",
                column: "EmployeeCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AssociationRequests_CompanyId",
                table: "AssociationRequests",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AssociationRequests_TargetUnionId",
                table: "AssociationRequests",
                column: "TargetUnionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCategories_UnionId",
                table: "EmployeeCategories",
                column: "UnionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCategories_UnionId1",
                table: "EmployeeCategories",
                column: "UnionId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AssociationRequests_Companies_CompanyId",
                table: "AssociationRequests",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AssociationRequests_Unions_TargetUnionId",
                table: "AssociationRequests",
                column: "TargetUnionId",
                principalTable: "Unions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalaryScales_EmployeeCategories_EmployeeCategoryId",
                table: "SalaryScales",
                column: "EmployeeCategoryId",
                principalTable: "EmployeeCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalaryScales_Unions_UnionId",
                table: "SalaryScales",
                column: "UnionId",
                principalTable: "Unions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssociationRequests_Companies_CompanyId",
                table: "AssociationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AssociationRequests_Unions_TargetUnionId",
                table: "AssociationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SalaryScales_EmployeeCategories_EmployeeCategoryId",
                table: "SalaryScales");

            migrationBuilder.DropForeignKey(
                name: "FK_SalaryScales_Unions_UnionId",
                table: "SalaryScales");

            migrationBuilder.DropTable(
                name: "EmployeeCategories");

            migrationBuilder.DropIndex(
                name: "IX_SalaryScales_EmployeeCategoryId",
                table: "SalaryScales");

            migrationBuilder.DropIndex(
                name: "IX_AssociationRequests_CompanyId",
                table: "AssociationRequests");

            migrationBuilder.DropIndex(
                name: "IX_AssociationRequests_TargetUnionId",
                table: "AssociationRequests");

            migrationBuilder.DropColumn(
                name: "LawContributionPercentage",
                table: "Unions");

            migrationBuilder.DropColumn(
                name: "UnionMemberPercentage",
                table: "Unions");

            migrationBuilder.DropColumn(
                name: "AntiguedadPercentage",
                table: "SalaryScales");

            migrationBuilder.DropColumn(
                name: "EmployeeCategoryId",
                table: "SalaryScales");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AssociationRequests");

            migrationBuilder.DropColumn(
                name: "TargetUnionId",
                table: "AssociationRequests");

            migrationBuilder.RenameColumn(
                name: "ProcessedAt",
                table: "AssociationRequests",
                newName: "ResolutionDate");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "AssociationRequests",
                newName: "RequestDate");

            migrationBuilder.RenameColumn(
                name: "AdminComment",
                table: "AssociationRequests",
                newName: "Observation");

            migrationBuilder.AlterColumn<int>(
                name: "UnionId",
                table: "SalaryScales",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "CategoryCode",
                table: "SalaryScales",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BusinessName",
                table: "AssociationRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanyCuit",
                table: "AssociationRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_SalaryScales_Unions_UnionId",
                table: "SalaryScales",
                column: "UnionId",
                principalTable: "Unions",
                principalColumn: "Id");
        }
    }
}
