using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GlowBook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfessionalWorkspaceCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkspaceName",
                table: "StaffMembers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkspaceName",
                table: "StaffMembers");
        }
    }
}
