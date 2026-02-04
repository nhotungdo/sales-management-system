using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Sales_Management.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "Avatar", "CreatedDate", "Email", "FullName", "GoogleId", "IsActive", "IsDeleted", "LastLogin", "PasswordHash", "PhoneNumber", "Role", "Username" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 1, 27, 0, 29, 5, 892, DateTimeKind.Local).AddTicks(4865), "admin@gmail.com", null, null, true, false, null, "$2a$11$4FuKu30gs49yGpxm.TC/J.75gR2W2BIHGadb7rW2uAhhClbwZyfqa", null, "Admin", "admin" },
                    { 2, null, new DateTime(2026, 1, 27, 0, 29, 6, 66, DateTimeKind.Local).AddTicks(8678), "sale@gmail.com", null, null, true, false, null, "$2a$11$V43Pg7/Dri0aWZ4c91JiCOGql4ASd39NVPQPW1CL9HLIk0T3D67.2", null, "Sale", "sale" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2);
        }
    }
}
