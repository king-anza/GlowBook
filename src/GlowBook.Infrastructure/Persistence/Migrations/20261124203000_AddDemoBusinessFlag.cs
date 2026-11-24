using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GlowBook.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GlowBookDbContext))]
[Migration("20261124203000_AddDemoBusinessFlag")]
public sealed class AddDemoBusinessFlag : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<bool>(name: "IsDemo", table: "Businesses", type: "boolean", nullable: false, defaultValue: false);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "IsDemo", table: "Businesses");
}
