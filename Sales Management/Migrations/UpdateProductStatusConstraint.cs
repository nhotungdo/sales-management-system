using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales_Management.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductStatusConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing check constraint on Status column dynamically
            migrationBuilder.Sql(@"
                DECLARE @constraintName NVARCHAR(200);
                SELECT TOP 1 @constraintName = obj.name
                FROM sys.check_constraints obj
                JOIN sys.columns col ON obj.parent_object_id = col.object_id AND col.column_id = obj.parent_column_id
                WHERE obj.parent_object_id = OBJECT_ID('Products') AND col.name = 'Status';

                IF @constraintName IS NOT NULL
                BEGIN
                    EXEC('ALTER TABLE Products DROP CONSTRAINT ' + @constraintName);
                END
            ");

            // Add new constraint
            migrationBuilder.Sql("ALTER TABLE Products ADD CONSTRAINT CK_Products_Status CHECK (Status IN ('Active', 'Inactive', 'Deleted'))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE Products DROP CONSTRAINT CK_Products_Status");
            // Note: We cannot restore the exact original system-generated name, but we restore the logic.
            // Assuming original allowed only Active and Inactive (based on error).
            migrationBuilder.Sql("ALTER TABLE Products ADD CONSTRAINT CK_Products_Status CHECK (Status IN ('Active', 'Inactive'))");
        }
    }
}
