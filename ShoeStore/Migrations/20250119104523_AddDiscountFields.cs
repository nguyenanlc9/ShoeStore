using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoeStore.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CouponDiscount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MemberDiscount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CouponDiscount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "MemberDiscount",
                table: "Orders");
        }
    }
}
