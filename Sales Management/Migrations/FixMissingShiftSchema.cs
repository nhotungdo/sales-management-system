using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales_Management.Migrations
{
    /// <inheritdoc />
    public partial class FixMissingShiftSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    ShiftId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.ShiftId);
                });

            migrationBuilder.AddColumn<int>(
                name: "ShiftId",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShiftId",
                table: "TimeAttendances",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ShiftId",
                table: "Employees",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeAttendances_ShiftId",
                table: "TimeAttendances",
                column: "ShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Shifts",
                table: "Employees",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "ShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeAttendances_Shifts",
                table: "TimeAttendances",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "ShiftId");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 4, 13, 58, 26, 893, DateTimeKind.Local).AddTicks(9206), "$2a$11$p04O72fW0vmp08Ge2AYNUufyWGeV0ldd4Ia7R.j9hc2VX6MLo2K26" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 4, 13, 58, 27, 26, DateTimeKind.Local).AddTicks(3780), "$2a$11$gIE9jo/3RF8bma1DZvtCtusjdr3LFm4oa3f4ubGOgwaIPljZfWAPq" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Shifts",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_TimeAttendances_Shifts",
                table: "TimeAttendances");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropIndex(
                name: "IX_Employees_ShiftId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_TimeAttendances_ShiftId",
                table: "TimeAttendances");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "TimeAttendances");

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
    }
}
