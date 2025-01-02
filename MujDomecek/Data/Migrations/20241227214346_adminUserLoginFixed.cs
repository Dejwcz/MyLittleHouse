using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MujDomecek.Data.Migrations
{
    /// <inheritdoc />
    public partial class adminUserLoginFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "98ab911b-bf8c-4181-8185-1a103a96a5b5",
                columns: new[] { "ConcurrencyStamp", "NormalizedUserName", "PasswordHash", "SecurityStamp", "UserName" },
                values: new object[] { "c1cb785d-cdd2-4f63-8f1a-216741728aac", "NFO@X213.CZ", "AQAAAAIAAYagAAAAELG0uQaCy3MQlC6pVFVUG0U6+ftQPw3LQjv8Q0THRKoK9tmsbjYOCxmATyKC9eUdEw==", "4a180b5d-8d59-4e6d-b858-884853284d3c", "info@x213.cz" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "98ab911b-bf8c-4181-8185-1a103a96a5b5",
                columns: new[] { "ConcurrencyStamp", "NormalizedUserName", "PasswordHash", "SecurityStamp", "UserName" },
                values: new object[] { "273d87e8-9662-4200-8d86-874552a82297", "DEJW", "AQAAAAIAAYagAAAAEJeDGLnYrVmk8T6teNuTQwaSdKplxFWENgwPYEtcXwQeiIduqQyitA7m89wrb/Rneg==", "9e6174a0-4784-4ee6-8f04-dd195be7cbc0", "Dejw" });
        }
    }
}
