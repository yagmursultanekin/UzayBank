using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UzayBank.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnchorQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnchorQueue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BlockchainTxHash = table.Column<string>(type: "nvarchar(66)", maxLength: 66, nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnchorQueue", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnchorQueue_IsProcessed_AccountId",
                table: "AnchorQueue",
                columns: new[] { "IsProcessed", "AccountId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnchorQueue");
        }
    }
}
