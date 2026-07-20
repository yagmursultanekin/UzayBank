using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UzayBank.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Accounts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "Accounts");
        }
    }
}
