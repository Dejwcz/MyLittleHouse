using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MujDomecek.Data.Migrations
{
    /// <inheritdoc />
    public partial class test : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "98ab911b-bf8c-4181-8185-1a103a96a5b5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "5f9ac7ee-6f33-460c-b554-87dfa3a57e7d", "AQAAAAIAAYagAAAAEMj//VUdTwkW6r1tqB+j4VBKwmNtFN9TDadAotBNtXjTSudRskTFAZ1dx+SUEl3jFA==", "4c753fab-8bff-4c58-8384-c7cda9e7fbba" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "98ab911b-bf8c-4181-8185-1a103a96a5b5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "ede76a92-ea1a-4786-9f59-fbf2417fe458", "AQAAAAIAAYagAAAAEHJ3mhdU5KNd59uNfg4oozPVbdn3QzXUCFoaKmcKrT1+W8bsNzqhOmXa25N/aKehuw==", "7043ac6f-cce9-4cf9-9030-722a6c4c6ce9" });
        }
    }
}
