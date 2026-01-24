using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddCoinPriceToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
             * Migration content commented out because the database already contains these changes,
             * but the EF Core Snapshot was out of sync.
             * Applying this empty migration will sync the history and snapshot without causing SQL errors.
             */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            /*
            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "VipPackages");

            migrationBuilder.DropColumn(
                name: "AmountMoney",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "TransactionCode",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "CoinPrice",
                table: "Products");

            migrationBuilder.AlterColumn<int>(
                name: "StockQuantity",
                table: "Products",
                type: "int",
                nullable: true,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);
             */
        }
    }
}
