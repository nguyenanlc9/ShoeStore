using Microsoft.EntityFrameworkCore.Migrations;

namespace ShoeStore.Data.Migrations
{
    public partial class RemoveRankColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rank",
                table: "Users");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Rank",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
} 