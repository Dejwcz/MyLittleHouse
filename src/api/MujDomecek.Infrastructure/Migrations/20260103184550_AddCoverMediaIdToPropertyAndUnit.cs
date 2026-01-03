using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MujDomecek.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoverMediaIdToPropertyAndUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CoverMediaId",
                table: "Units",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CoverMediaId",
                table: "Properties",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverMediaId",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "CoverMediaId",
                table: "Properties");
        }
    }
}
