using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MujDomecek.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdminAccountFromDBContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "45172101-2d51-4025-aa5d-f90eb130b904", "98ab911b-bf8c-4181-8185-1a103a96a5b5" });

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "98ab911b-bf8c-4181-8185-1a103a96a5b5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "FirstName", "LastName", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { "98ab911b-bf8c-4181-8185-1a103a96a5b5", 0, "5f9ac7ee-6f33-460c-b554-87dfa3a57e7d", "info@x213.cz", true, "Dejw", "", false, null, "INFO@X213.CZ", "INFO@X213.CZ", "AQAAAAIAAYagAAAAEMj//VUdTwkW6r1tqB+j4VBKwmNtFN9TDadAotBNtXjTSudRskTFAZ1dx+SUEl3jFA==", null, false, "4c753fab-8bff-4c58-8384-c7cda9e7fbba", false, "info@x213.cz" });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "45172101-2d51-4025-aa5d-f90eb130b904", "98ab911b-bf8c-4181-8185-1a103a96a5b5" });
        }
    }
}
