using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRGA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandSubscriptionRequestData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverageArea",
                table: "UnionSubscriptionRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Floor",
                table: "UnionSubscriptionRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Locality",
                table: "UnionSubscriptionRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "UnionSubscriptionRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReferencePersonFullName",
                table: "UnionSubscriptionRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "UnionSubscriptionRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StreetNumber",
                table: "UnionSubscriptionRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WhatsApp",
                table: "UnionSubscriptionRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverageArea",
                table: "UnionSubscriptionRequests");

            migrationBuilder.DropColumn(
                name: "Floor",
                table: "UnionSubscriptionRequests");

            migrationBuilder.DropColumn(
                name: "Locality",
                table: "UnionSubscriptionRequests");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "UnionSubscriptionRequests");

            migrationBuilder.DropColumn(
                name: "ReferencePersonFullName",
                table: "UnionSubscriptionRequests");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "UnionSubscriptionRequests");

            migrationBuilder.DropColumn(
                name: "StreetNumber",
                table: "UnionSubscriptionRequests");

            migrationBuilder.DropColumn(
                name: "WhatsApp",
                table: "UnionSubscriptionRequests");
        }
    }
}
