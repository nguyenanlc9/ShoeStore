using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShoeStore.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberRankAndTotalSpent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemberRanks",
                columns: table => new
                {
                    RankId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RankName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MinimumSpent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountPercent = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BadgeImage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberRanks", x => x.RankId);
                });

            migrationBuilder.AddColumn<int>(
                name: "MemberRankId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalSpent",
                table: "Users",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.InsertData(
                table: "MemberRanks",
                columns: new[] { "RankId", "BadgeImage", "Description", "DiscountPercent", "MinimumSpent", "RankName" },
                values: new object[,]
                {
                    { 1, null, "Thành viên mới", 0, 0m, "Bronze" },
                    { 2, null, "Giảm 5% mọi đơn hàng", 5, 5000000m, "Silver" },
                    { 3, null, "Giảm 10% mọi đơn hàng", 10, 20000000m, "Gold" },
                    { 4, null, "Giảm 15% mọi đơn hàng", 15, 50000000m, "Platinum" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_MemberRankId",
                table: "Users",
                column: "MemberRankId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_MemberRanks_MemberRankId",
                table: "Users",
                column: "MemberRankId",
                principalTable: "MemberRanks",
                principalColumn: "RankId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_MemberRanks_MemberRankId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "MemberRanks");

            migrationBuilder.DropIndex(
                name: "IX_Users_MemberRankId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MemberRankId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TotalSpent",
                table: "Users");
        }
    }
}