using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MujDomecek.Data.Migrations
{
    /// <inheritdoc />
    public partial class RedefinedRelationshipBetweenChildandParentUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Units_Units_UnitId",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Units_UnitId",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "Units");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "98ab911b-bf8c-4181-8185-1a103a96a5b5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "5d9d8545-2ad3-4b2e-b265-7927e48ab128", "AQAAAAIAAYagAAAAEO6oNAYTiw3Q/PBXnTef1YK2O5QMc0TJE8f8ELfAmKFpA71wYXus2OdiNihtbMcDZA==", "73d52232-58c0-4ad1-938e-b3e59c66bd67" });

            migrationBuilder.CreateIndex(
                name: "IX_Units_ParentUnitId",
                table: "Units",
                column: "ParentUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Units_Units_ParentUnitId",
                table: "Units",
                column: "ParentUnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Units_Units_ParentUnitId",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Units_ParentUnitId",
                table: "Units");

            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "Units",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "98ab911b-bf8c-4181-8185-1a103a96a5b5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "f9b7cdb0-7df6-4816-994f-319ff29e30c1", "AQAAAAIAAYagAAAAEOlKmte5dNa6O3taORx3oN88Q3l9NY+MQTxViQoXNnDh6/vhuJZapNFLYtbzVmJY9A==", "e9aafa5b-6512-4069-807c-6ec1c1690b3f" });

            migrationBuilder.CreateIndex(
                name: "IX_Units_UnitId",
                table: "Units",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Units_Units_UnitId",
                table: "Units",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id");
        }
    }
}
