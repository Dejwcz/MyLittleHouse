using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MujDomecek.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncModeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncAt",
                table: "Zaznamy",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SyncMode",
                table: "Zaznamy",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SyncStatus",
                table: "Zaznamy",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncAt",
                table: "Properties",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SyncMode",
                table: "Properties",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SyncStatus",
                table: "Properties",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncAt",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SyncMode",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SyncStatus",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ZaznamMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ZaznamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    PermissionsJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ServerRevision = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZaznamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZaznamMembers_Zaznamy_ZaznamId",
                        column: x => x.ZaznamId,
                        principalTable: "Zaznamy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ZaznamMembers_ZaznamId_UserId",
                table: "ZaznamMembers",
                columns: new[] { "ZaznamId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZaznamMembers");

            migrationBuilder.DropColumn(
                name: "LastSyncAt",
                table: "Zaznamy");

            migrationBuilder.DropColumn(
                name: "SyncMode",
                table: "Zaznamy");

            migrationBuilder.DropColumn(
                name: "SyncStatus",
                table: "Zaznamy");

            migrationBuilder.DropColumn(
                name: "LastSyncAt",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "SyncMode",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "SyncStatus",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "LastSyncAt",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "SyncMode",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "SyncStatus",
                table: "Projects");
        }
    }
}
