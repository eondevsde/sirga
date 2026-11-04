using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRGA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDatosDeMaesocAEmpleados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Employees",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressNumber",
                table: "Employees",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AffiliationNumber",
                table: "Employees",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Apartment",
                table: "Employees",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDate",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardNumber",
                table: "Employees",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentNumber",
                table: "Employees",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentTypeCode",
                table: "Employees",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Employees",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Floor",
                table: "Employees",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mobile",
                table: "Employees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Neighborhood",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Employees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sex",
                table: "Employees",
                type: "nvarchar(1)",
                maxLength: 1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkEmail",
                table: "Employees",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "Employees",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AddressNumber",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AffiliationNumber",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Apartment",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CardNumber",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DocumentNumber",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DocumentTypeCode",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Floor",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Mobile",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Neighborhood",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Sex",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "WorkEmail",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "Employees");
        }
    }
}
