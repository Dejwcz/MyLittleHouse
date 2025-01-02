using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MujDomecek.Data.Migrations
{
    /// <inheritdoc />
    public partial class PassHidenTest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "98ab911b-bf8c-4181-8185-1a103a96a5b5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "f1f5aefd-85d3-4434-a591-8a737f7e61a3", "AQAAAAIAAYagAAAAEGYVjue012fYPGjTHRkpDiswVCSsaO+zJRInA7myiSiKXMx9KRyyn66shfedNSobuQ==", "ffd48a24-3abe-404c-8368-94b3655f0465" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "98ab911b-bf8c-4181-8185-1a103a96a5b5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "5d9d8545-2ad3-4b2e-b265-7927e48ab128", "AQAAAAIAAYagAAAAEO6oNAYTiw3Q/PBXnTef1YK2O5QMc0TJE8f8ELfAmKFpA71wYXus2OdiNihtbMcDZA==", "73d52232-58c0-4ad1-938e-b3e59c66bd67" });
        }
    }
}
