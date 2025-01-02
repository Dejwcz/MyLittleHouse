using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MujDomecek.Data.Migrations
{
    /// <inheritdoc />
    public partial class CostIsIntegerNowBecause : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Cost",
                table: "Repairs",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "98ab911b-bf8c-4181-8185-1a103a96a5b5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "ede76a92-ea1a-4786-9f59-fbf2417fe458", "AQAAAAIAAYagAAAAEHJ3mhdU5KNd59uNfg4oozPVbdn3QzXUCFoaKmcKrT1+W8bsNzqhOmXa25N/aKehuw==", "7043ac6f-cce9-4cf9-9030-722a6c4c6ce9" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Cost",
                table: "Repairs",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "98ab911b-bf8c-4181-8185-1a103a96a5b5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "702cd58b-2976-49ea-b07d-cfbc8b5d1721", "AQAAAAIAAYagAAAAELuPWO5fSZRoQNDuhJF51MN5zIF3ZePlvXn5++JsaNqrA79km51ByqGCGhGdflmPQg==", "386d4fca-fc1a-4d99-86c8-78ccb65bacd6" });
        }
    }
}
