using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MujDomecek.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyTypeAndUpdateUnitType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PropertyType",
                table: "Properties",
                type: "integer",
                nullable: false,
                defaultValue: 6);

            migrationBuilder.Sql(
                "UPDATE \"Units\" SET \"UnitType\" = CASE " +
                "WHEN \"UnitType\" = 4 THEN 0 " +
                "WHEN \"UnitType\" = 5 THEN 1 " +
                "WHEN \"UnitType\" = 2 THEN 3 " +
                "ELSE 4 END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PropertyType",
                table: "Properties");
        }
    }
}
