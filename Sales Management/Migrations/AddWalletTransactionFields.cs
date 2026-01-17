using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletTransactionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountMoney",
                table: "WalletTransactions",
                type: "decimal(15,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionCode",
                table: "WalletTransactions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountMoney",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "TransactionCode",
                table: "WalletTransactions");
        }
    }
}
