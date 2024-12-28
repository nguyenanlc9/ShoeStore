using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoeStore.Migrations
{
    /// <inheritdoc />
    public partial class AddContactUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactUsers",
                columns: table => new
                {
                    ContactUId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContactUFirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContactULastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContactUEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactUAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactUPhone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    ContactUMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactUsers", x => x.ContactUId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactUsers");
        }
    }
}
