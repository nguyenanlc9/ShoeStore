using Microsoft.EntityFrameworkCore.Migrations;
using ShoeStore.Models.Enums;

namespace ShoeStore.Data.Migrations
{
    public partial class AddUserRankColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<UserRank>(
                name: "Rank",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: (int)UserRank.Bronze);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rank",
                table: "Users");
        }
    }
} 