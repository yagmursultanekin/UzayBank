using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UzayBank.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionRef : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TransactionRef",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Mevcut kayıtlara benzersiz GUID atıyoruz.
            //
            // NEDEN GEREKLİ: Kolon eklendiğinde tüm satırlar aynı varsayılan değeri
            // (Guid.Empty) alır. Hemen ardından unique index oluşturulmaya çalışılırsa
            // mükerrer kayıt hatası verir ve migration yarıda kalır.
            //
            // NEWID() SQL Server'ın GUID üreteci — her satır için ayrı çağrılır.
            migrationBuilder.Sql("UPDATE Transactions SET TransactionRef = NEWID();");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransactionRef",
                table: "Transactions",
                column: "TransactionRef",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_TransactionRef",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TransactionRef",
                table: "Transactions");
        }
    }
}
