using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddCreatedAtToOrder : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "CreatedAt",
            table: "Orders",
            type: "datetime2",
            nullable: false,
            defaultValueSql: "GETDATE()");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CreatedAt",
            table: "Orders");
    }
} 