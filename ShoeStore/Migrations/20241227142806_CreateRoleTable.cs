using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoeStore.Migrations
{
    /// <inheritdoc />
    public partial class CreateRoleTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tạo bảng Roles
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleID);
                });

            // Thêm cột RoleID vào bảng Users
            migrationBuilder.AddColumn<int>(
                name: "RoleID",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 2); // Mặc định là User role

            // Thêm foreign key từ Users đến Roles
            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleID",
                table: "Users",
                column: "RoleID",
                principalTable: "Roles",
                principalColumn: "RoleID",
                onDelete: ReferentialAction.Restrict);

            // Thêm data cho Roles
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleID", "RoleName" },
                values: new object[,]
                {
                { 1, "User" },
                { 2, "Admin" }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Xóa foreign key
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleID",
                table: "Users");

            // Xóa cột RoleID từ bảng Users
            migrationBuilder.DropColumn(
                name: "RoleID",
                table: "Users");

            // Xóa bảng Roles
            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
