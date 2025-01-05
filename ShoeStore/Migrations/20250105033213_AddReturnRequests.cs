using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoeStore.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Brands_BrandId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSizeStockHistories_Products_ProductId",
                table: "ProductSizeStockHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSizeStockHistories_Sizes_SizeId",
                table: "ProductSizeStockHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSizeStocks_Products_ProductID",
                table: "ProductSizeStocks");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSizeStocks_Sizes_SizeID",
                table: "ProductSizeStocks");

            migrationBuilder.CreateTable(
                name: "ReturnRequests",
                columns: table => new
                {
                    ReturnId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Images = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AdminNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnRequests", x => x.ReturnId);
                    table.ForeignKey(
                        name: "FK_ReturnRequests_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_OrderId",
                table: "ReturnRequests",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_UserId",
                table: "ReturnRequests",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Brands_BrandId",
                table: "Products",
                column: "BrandId",
                principalTable: "Brands",
                principalColumn: "BrandId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSizeStockHistories_Products_ProductId",
                table: "ProductSizeStockHistories",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSizeStockHistories_Sizes_SizeId",
                table: "ProductSizeStockHistories",
                column: "SizeId",
                principalTable: "Sizes",
                principalColumn: "SizeID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSizeStocks_Products_ProductID",
                table: "ProductSizeStocks",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ProductId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSizeStocks_Sizes_SizeID",
                table: "ProductSizeStocks",
                column: "SizeID",
                principalTable: "Sizes",
                principalColumn: "SizeID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Brands_BrandId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSizeStockHistories_Products_ProductId",
                table: "ProductSizeStockHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSizeStockHistories_Sizes_SizeId",
                table: "ProductSizeStockHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSizeStocks_Products_ProductID",
                table: "ProductSizeStocks");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSizeStocks_Sizes_SizeID",
                table: "ProductSizeStocks");

            migrationBuilder.DropTable(
                name: "ReturnRequests");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Brands_BrandId",
                table: "Products",
                column: "BrandId",
                principalTable: "Brands",
                principalColumn: "BrandId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSizeStockHistories_Products_ProductId",
                table: "ProductSizeStockHistories",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSizeStockHistories_Sizes_SizeId",
                table: "ProductSizeStockHistories",
                column: "SizeId",
                principalTable: "Sizes",
                principalColumn: "SizeID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSizeStocks_Products_ProductID",
                table: "ProductSizeStocks",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSizeStocks_Sizes_SizeID",
                table: "ProductSizeStocks",
                column: "SizeID",
                principalTable: "Sizes",
                principalColumn: "SizeID");
        }
    }
}
