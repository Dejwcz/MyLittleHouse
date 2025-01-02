using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MujDomecek.Data.Migrations
{
    /// <inheritdoc />
    public partial class DungeonMasterAndRolesAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "45172101-2d51-4025-aa5d-f90eb130b904", null, "DungeonMaster", "DUNGEONMASTER" },
                    { "be221c97-926c-4ea0-886e-38134cdd90d2", null, "Supervisor", "SUPERVISOR" },
                    { "c45f0814-7e4a-454b-98fb-61a534a6e19a", null, "User", "USER" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "FirstName", "LastName", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { "98ab911b-bf8c-4181-8185-1a103a96a5b5", 0, "273d87e8-9662-4200-8d86-874552a82297", "info@x213.cz", true, "Dejw", "", false, null, "INFO@X213.CZ", "DEJW", "AQAAAAIAAYagAAAAEJeDGLnYrVmk8T6teNuTQwaSdKplxFWENgwPYEtcXwQeiIduqQyitA7m89wrb/Rneg==", null, false, "9e6174a0-4784-4ee6-8f04-dd195be7cbc0", false, "Dejw" });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "45172101-2d51-4025-aa5d-f90eb130b904", "98ab911b-bf8c-4181-8185-1a103a96a5b5" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "be221c97-926c-4ea0-886e-38134cdd90d2");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "c45f0814-7e4a-454b-98fb-61a534a6e19a");

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "45172101-2d51-4025-aa5d-f90eb130b904", "98ab911b-bf8c-4181-8185-1a103a96a5b5" });

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "45172101-2d51-4025-aa5d-f90eb130b904");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "98ab911b-bf8c-4181-8185-1a103a96a5b5");
        }
    }
}
