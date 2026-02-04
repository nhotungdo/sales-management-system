using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceCentsToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PriceCents",
                table: "Products",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 4, 21, 44, 37, 661, DateTimeKind.Local).AddTicks(7296), "$2a$11$AuC26NorVAtOIchnst5oc.02zBLtOQH.oZDWBiPBjfooUOGft1ng." });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 4, 21, 44, 37, 785, DateTimeKind.Local).AddTicks(6790), "$2a$11$5i8MZchCAbV8Q3vqLNjMMOd7.FKh4VN/9MVczTgO8DSC1JMnruTdS" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceCents",
                table: "Products");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 4, 21, 23, 51, 497, DateTimeKind.Local).AddTicks(9862), "$2a$11$JlSOstXdYsXWXIi8.K5cQOx1CLonwcrra4SfnqX386Sa1XwNfZEWu" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 4, 21, 23, 51, 624, DateTimeKind.Local).AddTicks(720), "$2a$11$DKWK9LiD1SWyUHUDqAiEFOAt1ZwNAFjNT4LQ37Wj9bAmQXFevnpji" });
        }
    }
}
