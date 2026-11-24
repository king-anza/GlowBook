using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GlowBook.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GlowBookDbContext))]
[Migration("20261124190000_AddBusinessLocationCoordinates")]
public sealed class AddBusinessLocationCoordinates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<double>(name: "Latitude", table: "BusinessLocations", type: "double precision", nullable: true);
        migrationBuilder.AddColumn<double>(name: "Longitude", table: "BusinessLocations", type: "double precision", nullable: true);
        migrationBuilder.AddCheckConstraint(name: "CK_BusinessLocation_Latitude", table: "BusinessLocations", sql: "\"Latitude\" IS NULL OR \"Latitude\" BETWEEN -90 AND 90");
        migrationBuilder.AddCheckConstraint(name: "CK_BusinessLocation_Longitude", table: "BusinessLocations", sql: "\"Longitude\" IS NULL OR \"Longitude\" BETWEEN -180 AND 180");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "CK_BusinessLocation_Latitude", table: "BusinessLocations");
        migrationBuilder.DropCheckConstraint(name: "CK_BusinessLocation_Longitude", table: "BusinessLocations");
        migrationBuilder.DropColumn(name: "Latitude", table: "BusinessLocations");
        migrationBuilder.DropColumn(name: "Longitude", table: "BusinessLocations");
    }
}
