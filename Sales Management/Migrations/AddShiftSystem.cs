using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 4, 13, 51, 52, 509, DateTimeKind.Local).AddTicks(4961), "$2a$11$DpidXXubWLmb3hc7tnJVq.hnuuq5P9VVK0chICByTJwN6aEiMuGEe" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 4, 13, 51, 52, 635, DateTimeKind.Local).AddTicks(299), "$2a$11$WDPim1bNl/B0MvNlGwSv6.8BXG98z5qu1sWvYlIsRuqMO59ZZ.qau" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 4, 13, 13, 10, 699, DateTimeKind.Local).AddTicks(1382), "$2a$11$UDL3DL.22jOytAoQNZzMWuWqWced6RIW99rdLFerCev.kIqSwWs2C" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 4, 13, 13, 10, 835, DateTimeKind.Local).AddTicks(2679), "$2a$11$MQ2rKIGAf3c4fpaodI312uTEi3ky3.eIBOW8Glc72rOgK6jreWzdO" });
        }
    }
}
