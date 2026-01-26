using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Sales_Management.Migrations
{
    /// <inheritdoc />
    public partial class SeedCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "Description", "DisplayOrder", "ImageUrl", "Name", "ParentId", "Status" },
                values: new object[,]
                {
                    { 1, null, null, null, "Đồ uống", null, "Active" },
                    { 2, null, null, null, "Đồ ăn vặt", null, "Active" },
                    { 3, null, null, null, "Thực phẩm", null, "Active" },
                    { 4, null, null, null, "Điện tử", null, "Active" },
                    { 5, null, null, null, "Thời trang", null, "Active" },
                    { 6, null, null, null, "Đồ gia dụng", null, "Active" },
                    { 7, null, null, null, "Sách", null, "Active" }
                });

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
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 1, 27, 0, 45, 32, 758, DateTimeKind.Local).AddTicks(77), "$2a$11$7FmMeAQEAGDD9N81v5Wg0O8BK89oMWr4dnTVp7jaNVRNLi48hze3G" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 7);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 1, 27, 0, 29, 5, 892, DateTimeKind.Local).AddTicks(4865), "$2a$11$4FuKu30gs49yGpxm.TC/J.75gR2W2BIHGadb7rW2uAhhClbwZyfqa" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 1, 27, 0, 29, 6, 66, DateTimeKind.Local).AddTicks(8678), "$2a$11$V43Pg7/Dri0aWZ4c91JiCOGql4ASd39NVPQPW1CL9HLIk0T3D67.2" });
        }
    }
}
