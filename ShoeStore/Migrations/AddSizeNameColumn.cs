using Microsoft.EntityFrameworkCore.Migrations;

namespace ShoeStore.Migrations
{
    public partial class AddSizeNameColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SizeName",
                table: "Sizes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Cập nhật dữ liệu cho cột mới
            migrationBuilder.Sql(@"
                UPDATE Sizes 
                SET SizeName = CAST(SizeValue AS VARCHAR(10))
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SizeName",
                table: "Sizes");
        }
    }
} 