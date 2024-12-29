using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoeStore.Migrations
{
    /// <inheritdoc />
    public partial class AddSizeNameToSizes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           

            migrationBuilder.AddColumn<string>(
                name: "SizeName",
                table: "Sizes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SizeName",
                table: "Sizes");

        }
    }
}
