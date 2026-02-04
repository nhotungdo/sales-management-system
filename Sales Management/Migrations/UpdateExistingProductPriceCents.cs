using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales_Management.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExistingProductPriceCents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 4, 21, 47, 53, 860, DateTimeKind.Local).AddTicks(7524), "$2a$11$YE68K2Qi9lSCuL/PdfW6wuzQOJL8GljZEl3Yt1HEOo8besI7Xosh6" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 4, 21, 47, 53, 982, DateTimeKind.Local).AddTicks(7735), "$2a$11$HM6/PnhoSZWAXgBYE7qTGe0Y61om/tb4nElZ31EVGAw5ajZBWoRoO" });

            // Update PriceCents for existing products (1000 VND = 1 Cent)
            migrationBuilder.Sql(@"
                UPDATE Products 
                SET PriceCents = ROUND(SellingPrice / 1000.0, 2)
                WHERE PriceCents IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
