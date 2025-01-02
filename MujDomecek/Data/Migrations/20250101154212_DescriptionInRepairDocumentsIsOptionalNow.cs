using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MujDomecek.Data.Migrations
{
    /// <inheritdoc />
    public partial class DescriptionInRepairDocumentsIsOptionalNow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "RepairDocuments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "98ab911b-bf8c-4181-8185-1a103a96a5b5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "702cd58b-2976-49ea-b07d-cfbc8b5d1721", "AQAAAAIAAYagAAAAELuPWO5fSZRoQNDuhJF51MN5zIF3ZePlvXn5++JsaNqrA79km51ByqGCGhGdflmPQg==", "386d4fca-fc1a-4d99-86c8-78ccb65bacd6" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "RepairDocuments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "98ab911b-bf8c-4181-8185-1a103a96a5b5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "f1f5aefd-85d3-4434-a591-8a737f7e61a3", "AQAAAAIAAYagAAAAEGYVjue012fYPGjTHRkpDiswVCSsaO+zJRInA7myiSiKXMx9KRyyn66shfedNSobuQ==", "ffd48a24-3abe-404c-8368-94b3655f0465" });
        }
    }
}
