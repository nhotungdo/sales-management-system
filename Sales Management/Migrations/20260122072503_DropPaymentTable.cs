using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales_Management.Migrations
{
    /// <inheritdoc />
    public partial class DropPaymentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.AddColumn<int>(
                name: "InvoiceId",
                table: "WalletTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_InvoiceId",
                table: "WalletTransactions",
                column: "InvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_WalletTransactions_Invoices_InvoiceId",
                table: "WalletTransactions",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "InvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WalletTransactions_Invoices_InvoiceId",
                table: "WalletTransactions");

            migrationBuilder.DropIndex(
                name: "IX_WalletTransactions_InvoiceId",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "WalletTransactions");

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    PaymentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_Payments_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "InvoiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InvoiceId",
                table: "Payments",
                column: "InvoiceId",
                unique: true);
        }
    }
}
