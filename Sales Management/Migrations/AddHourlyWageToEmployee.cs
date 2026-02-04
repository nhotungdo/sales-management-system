using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddHourlyWageToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HourlyWage",
                table: "Employees",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Employees",
                keyColumn: "EmployeeId",
                keyValue: 1,
                column: "HourlyWage",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 4, 20, 48, 7, 31, DateTimeKind.Local).AddTicks(7010), "$2a$11$t2i1qFa43L9wTT36wnMoWO8VVTfmAOuCdQifj3AvLR.FZRK./jc1S" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 4, 20, 48, 7, 163, DateTimeKind.Local).AddTicks(8188), "$2a$11$N6i7Sh/Gec8RhKWKxWfEueB3.y4S/zqFJOX2fBKSpiuy2GOQ0G.f6" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HourlyWage",
                table: "Employees");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 4, 20, 13, 34, 271, DateTimeKind.Local).AddTicks(8333), "$2a$11$ZnIG2vSD8ijgjxTTrsitXuyWgeFouyqq1lUexvBcUoFY45p2uaDTa" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 4, 20, 13, 34, 397, DateTimeKind.Local).AddTicks(3414), "$2a$11$TiCjVaR3rpbH1RnQ1dQzduxtDrai4JwiVQVWoSn6ewJRdeOHYqZoy" });
        }
    }
}
