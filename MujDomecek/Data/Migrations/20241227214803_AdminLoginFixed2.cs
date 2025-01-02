using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MujDomecek.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdminLoginFixed2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "98ab911b-bf8c-4181-8185-1a103a96a5b5",
                columns: new[] { "ConcurrencyStamp", "NormalizedUserName", "PasswordHash", "SecurityStamp" },
                values: new object[] { "b47a0ec8-84cb-4aae-b252-040023fecd17", "INFO@X213.CZ", "AQAAAAIAAYagAAAAEJQQsm84LSivBlzRd4vK7Eq/RZHgtB0b9bQcUbhBlME54yiZ/DnVms06Z9f5v6Cieg==", "c7e41a58-75db-4a1c-994b-4dd5fd4bf173" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "98ab911b-bf8c-4181-8185-1a103a96a5b5",
                columns: new[] { "ConcurrencyStamp", "NormalizedUserName", "PasswordHash", "SecurityStamp" },
                values: new object[] { "c1cb785d-cdd2-4f63-8f1a-216741728aac", "NFO@X213.CZ", "AQAAAAIAAYagAAAAELG0uQaCy3MQlC6pVFVUG0U6+ftQPw3LQjv8Q0THRKoK9tmsbjYOCxmATyKC9eUdEw==", "4a180b5d-8d59-4e6d-b858-884853284d3c" });
        }
    }
}
