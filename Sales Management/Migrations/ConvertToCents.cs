using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales_Management.Migrations
{
    /// <inheritdoc />
    public partial class ConvertToCents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Products SET SellingPrice = SellingPrice / 100.0, ImportPrice = ImportPrice / 100.0");
            migrationBuilder.Sql("UPDATE OrderDetails SET UnitPrice = UnitPrice / 100.0, Total = Total / 100.0");
            migrationBuilder.Sql("UPDATE Orders SET SubTotal = SubTotal / 100.0, TaxAmount = TaxAmount / 100.0, DiscountAmount = DiscountAmount / 100.0, TotalAmount = TotalAmount / 100.0");
            migrationBuilder.Sql("UPDATE Invoices SET Amount = Amount / 100.0");
            migrationBuilder.Sql("UPDATE Wallets SET Balance = Balance / 100.0");
            migrationBuilder.Sql("UPDATE WalletTransactions SET Amount = Amount / 100.0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Products SET SellingPrice = SellingPrice * 100.0, ImportPrice = ImportPrice * 100.0");
            migrationBuilder.Sql("UPDATE OrderDetails SET UnitPrice = UnitPrice * 100.0, Total = Total * 100.0");
            migrationBuilder.Sql("UPDATE Orders SET SubTotal = SubTotal * 100.0, TaxAmount = TaxAmount * 100.0, DiscountAmount = DiscountAmount * 100.0, TotalAmount = TotalAmount * 100.0");
            migrationBuilder.Sql("UPDATE Invoices SET Amount = Amount * 100.0");
            migrationBuilder.Sql("UPDATE Wallets SET Balance = Balance * 100.0");
            migrationBuilder.Sql("UPDATE WalletTransactions SET Amount = Amount * 100.0");
        }
    }
}
