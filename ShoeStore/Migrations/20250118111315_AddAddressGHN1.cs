using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoeStore.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressGHN1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistrictName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ProvinceId",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "WardName",
                table: "Orders",
                newName: "ProvinceCode");

            migrationBuilder.RenameColumn(
                name: "ProvinceName",
                table: "Orders",
                newName: "DistrictCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProvinceCode",
                table: "Orders",
                newName: "WardName");

            migrationBuilder.RenameColumn(
                name: "DistrictCode",
                table: "Orders",
                newName: "ProvinceName");

            migrationBuilder.AddColumn<string>(
                name: "DistrictName",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProvinceId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
