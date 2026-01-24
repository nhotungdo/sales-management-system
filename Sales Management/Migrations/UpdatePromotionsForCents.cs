using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales_Management.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePromotionsForCents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Promotions SET MinOrderValue = MinOrderValue / 100.0, MaxDiscountAmount = MaxDiscountAmount / 100.0");
            migrationBuilder.Sql("UPDATE Promotions SET Value = Value / 100.0 WHERE DiscountType = 'Amount'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Promotions SET MinOrderValue = MinOrderValue * 100.0, MaxDiscountAmount = MaxDiscountAmount * 100.0");
            migrationBuilder.Sql("UPDATE Promotions SET Value = Value * 100.0 WHERE DiscountType = 'Amount'");
        }
    }
}
