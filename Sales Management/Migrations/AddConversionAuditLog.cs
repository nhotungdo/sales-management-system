using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddConversionAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if table exists before creating
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ConversionAuditLogs')
                BEGIN
                    CREATE TABLE [ConversionAuditLogs] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [VndAmount] decimal(18,2) NOT NULL,
                        [CentsAmount] decimal(18,2) NOT NULL,
                        [ConversionRate] decimal(18,4) NOT NULL,
                        [Timestamp] datetime2 NOT NULL,
                        [IpAddress] nvarchar(50) NOT NULL,
                        [IsSuccess] bit NOT NULL,
                        [ErrorMessage] nvarchar(max) NOT NULL,
                        CONSTRAINT [PK_ConversionAuditLogs] PRIMARY KEY ([Id])
                    );
                END
            ");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversionAuditLogs");

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
    }
}
