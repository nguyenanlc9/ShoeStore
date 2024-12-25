using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoeStore.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductSizeModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductSizeStocks",
                columns: table => new
                {
                    ProductSizeStockID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    SizeID = table.Column<int>(type: "int", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSizeStocks", x => x.ProductSizeStockID);
                    table.ForeignKey(
                        name: "FK_ProductSizeStocks_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductSizeStocks_Sizes_SizeID",
                        column: x => x.SizeID,
                        principalTable: "Sizes",
                        principalColumn: "SizeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSizeStocks_ProductID",
                table: "ProductSizeStocks",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSizeStocks_SizeID",
                table: "ProductSizeStocks",
                column: "SizeID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductSizeStocks");
        }
    }
}
