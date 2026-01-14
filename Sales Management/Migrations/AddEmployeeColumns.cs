using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractType",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractFile",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ChangeHistory",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);
                 
            // Assumes Payrolls and TimeAttendances might exist or will be handled if error arises.
            // Keeping them out of this migration prevents 'Table already exists' crash.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
             migrationBuilder.DropColumn(name: "Department", table: "Employees");
             migrationBuilder.DropColumn(name: "ContractType", table: "Employees");
             migrationBuilder.DropColumn(name: "ContractFile", table: "Employees");
             migrationBuilder.DropColumn(name: "IsDeleted", table: "Employees");
             migrationBuilder.DropColumn(name: "ChangeHistory", table: "Employees");
        }
    }
}
