using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales_Management.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVipPackagesForCents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE VipPackages SET Price = Price / 100.0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE VipPackages SET Price = Price * 100.0");
        }
    }
}
