using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MujDomecek.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameZaznamDokumentToMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ZaznamDokumenty_Zaznamy_ZaznamId",
                table: "ZaznamDokumenty");

            migrationBuilder.DropIndex(
                name: "IX_ZaznamDokumenty_ZaznamId",
                table: "ZaznamDokumenty");

            migrationBuilder.RenameTable(
                name: "ZaznamDokumenty",
                newName: "Media");

            migrationBuilder.AddColumn<int>(
                name: "OwnerType",
                table: "Media",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Media",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.Sql("UPDATE \"Media\" SET \"OwnerId\" = \"ZaznamId\"");

            migrationBuilder.DropColumn(
                name: "ZaznamId",
                table: "Media");

            migrationBuilder.CreateIndex(
                name: "IX_Media_OwnerType_OwnerId",
                table: "Media",
                columns: new[] { "OwnerType", "OwnerId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ZaznamId",
                table: "Media",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.Sql("UPDATE \"Media\" SET \"ZaznamId\" = \"OwnerId\"");

            migrationBuilder.DropIndex(
                name: "IX_Media_OwnerType_OwnerId",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "OwnerType",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Media");

            migrationBuilder.RenameTable(
                name: "Media",
                newName: "ZaznamDokumenty");

            migrationBuilder.CreateIndex(
                name: "IX_ZaznamDokumenty_ZaznamId",
                table: "ZaznamDokumenty",
                column: "ZaznamId");

            migrationBuilder.AddForeignKey(
                name: "FK_ZaznamDokumenty_Zaznamy_ZaznamId",
                table: "ZaznamDokumenty",
                column: "ZaznamId",
                principalTable: "Zaznamy",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
