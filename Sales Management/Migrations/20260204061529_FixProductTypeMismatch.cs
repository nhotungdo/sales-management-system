using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales_Management.Migrations
{
    /// <inheritdoc />
    public partial class FixProductTypeMismatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "CoinPrice",
                table: "Products",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(15,2)",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "EmployeeId", "BasicSalary", "ChangeHistory", "ContractFile", "ContractType", "Department", "IsDeleted", "Position", "StartWorkingDate", "UserId" },
                values: new object[] { 1, 5000000m, null, null, "Full-time", "Sales", false, "Sales Staff", new DateOnly(2026, 2, 4), 2 });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 4, 13, 15, 26, 872, DateTimeKind.Local).AddTicks(1292), "$2a$11$Gi5FgUhh5HGrqSscqluiseHbVe0s1D9hnEz8oQSfnsPtbhj.foGJu" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                columns: new[] { "CreatedDate", "PasswordHash", "Role" },
                values: new object[] { new DateTime(2026, 2, 4, 13, 15, 27, 154, DateTimeKind.Local).AddTicks(6590), "$2a$11$2jB8X7I/wi8jw73X5GDc8uAuM61sS.7my6J4HGgJJa1A3oeLFmoSm", "Sales" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "EmployeeId",
                keyValue: 1);

            migrationBuilder.AlterColumn<decimal>(
                name: "CoinPrice",
                table: "Products",
                type: "decimal(15,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 1, 27, 0, 45, 32, 573, DateTimeKind.Local).AddTicks(5684), "$2a$11$415nmOGi2bULzpMmqH03M.Ts2OyT2SL/pAH8tch4J3wtgcezXbybG" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                columns: new[] { "CreatedDate", "PasswordHash", "Role" },
                values: new object[] { new DateTime(2026, 1, 27, 0, 45, 32, 758, DateTimeKind.Local).AddTicks(77), "$2a$11$7FmMeAQEAGDD9N81v5Wg0O8BK89oMWr4dnTVp7jaNVRNLi48hze3G", "Sale" });
        }
    }
}
