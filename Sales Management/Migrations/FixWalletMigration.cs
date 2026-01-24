using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales_Management.Migrations
{
    /// <inheritdoc />
    public partial class FixWalletMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Revert Coin Quantity (Balance and Amount were coins, unrelated to currency conversion)
            migrationBuilder.Sql("UPDATE Wallets SET Balance = Balance * 100.0");
            migrationBuilder.Sql("UPDATE WalletTransactions SET Amount = Amount * 100.0");
            
            // Convert Money Amount to Cents
            migrationBuilder.Sql("UPDATE WalletTransactions SET AmountMoney = AmountMoney / 100.0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Wallets SET Balance = Balance / 100.0");
            migrationBuilder.Sql("UPDATE WalletTransactions SET Amount = Amount / 100.0");
            migrationBuilder.Sql("UPDATE WalletTransactions SET AmountMoney = AmountMoney * 100.0");
        }
    }
}
