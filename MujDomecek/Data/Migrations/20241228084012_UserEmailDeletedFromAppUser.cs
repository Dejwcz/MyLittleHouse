using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MujDomecek.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserEmailDeletedFromAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "98ab911b-bf8c-4181-8185-1a103a96a5b5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "3f904394-3187-4b73-be7c-9da995fa7b55", "AQAAAAIAAYagAAAAEBYH6ohDplag/wfEETsJ5p/Jug1eAvCREEV05EWc+36NAAf8QA/b9qTaBN5bVz6JVA==", "b4845384-8895-4fae-a63b-633552a268eb" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "98ab911b-bf8c-4181-8185-1a103a96a5b5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "b47a0ec8-84cb-4aae-b252-040023fecd17", "AQAAAAIAAYagAAAAEJQQsm84LSivBlzRd4vK7Eq/RZHgtB0b9bQcUbhBlME54yiZ/DnVms06Z9f5v6Cieg==", "c7e41a58-75db-4a1c-994b-4dd5fd4bf173" });
        }
    }
}
