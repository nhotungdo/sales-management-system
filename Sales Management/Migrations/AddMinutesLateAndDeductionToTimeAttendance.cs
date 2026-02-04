using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddMinutesLateAndDeductionToTimeAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeductionAmount",
                table: "TimeAttendances",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MinutesLate",
                table: "TimeAttendances",
                type: "int",
                nullable: false,
                defaultValue: 0);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeductionAmount",
                table: "TimeAttendances");

            migrationBuilder.DropColumn(
                name: "MinutesLate",
                table: "TimeAttendances");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 4, 17, 48, 25, 264, DateTimeKind.Local).AddTicks(2534), "$2a$11$YUNj8mkDOA56W9mqMjwut.sXl.lF1YQ6dkl6YlD/lv5aItNfQjFzu" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 4, 17, 48, 25, 389, DateTimeKind.Local).AddTicks(3005), "$2a$11$WeLoTlD/ZFX4HACPx0.nEOBpOLR9EZILGpCqgE9N2STjVFILQ3OYe" });
        }
    }
}
