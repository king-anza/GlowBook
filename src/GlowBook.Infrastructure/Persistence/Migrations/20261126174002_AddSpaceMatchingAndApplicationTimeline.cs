using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GlowBook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpaceMatchingAndApplicationTimeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"WorkspaceApplications\" SET \"Status\" = 'Submitted' WHERE \"Status\" = 'Pending'");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipientUserId_EventType_SubjectId",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId_EventType_SubjectId",
                table: "Notifications",
                columns: new[] { "RecipientUserId", "EventType", "SubjectId" },
                unique: true,
                filter: "\"EventType\" IN ('BookingReminder', 'SpaceMatch')");
            migrationBuilder.AddColumn<string>(
                name: "Availability",
                table: "WorkspaceApplications",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProfileSummary",
                table: "WorkspaceApplications",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Skills",
                table: "WorkspaceApplications",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "WorkspaceApplicantProfiles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Skills = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Availability = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AvailableFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    PortfolioUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    PreferredCities = table.Column<string[]>(type: "text[]", nullable: false),
                    PreferredOpportunityTypes = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MatchingAlertsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceApplicantProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_WorkspaceApplicantProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceApplicationStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceApplicationStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceApplicationStatusHistory_AspNetUsers_ChangedByUser~",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkspaceApplicationStatusHistory_WorkspaceApplications_App~",
                        column: x => x.ApplicationId,
                        principalTable: "WorkspaceApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceApplicationStatusHistory_ApplicationId_ChangedAtUtc",
                table: "WorkspaceApplicationStatusHistory",
                columns: new[] { "ApplicationId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceApplicationStatusHistory_ChangedByUserId",
                table: "WorkspaceApplicationStatusHistory",
                column: "ChangedByUserId");

            migrationBuilder.Sql("""
                INSERT INTO "WorkspaceApplicationStatusHistory" ("Id", "ApplicationId", "Status", "ChangedByUserId", "ChangedAtUtc")
                SELECT gen_random_uuid(), "Id", "Status", "ApplicantUserId", "CreatedAtUtc" FROM "WorkspaceApplications"
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"WorkspaceApplications\" SET \"Status\" = 'Pending' WHERE \"Status\" IN ('Submitted', 'Viewed')");
            migrationBuilder.Sql("UPDATE \"WorkspaceApplications\" SET \"Status\" = 'Accepted' WHERE \"Status\" = 'Filled'");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipientUserId_EventType_SubjectId",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId_EventType_SubjectId",
                table: "Notifications",
                columns: new[] { "RecipientUserId", "EventType", "SubjectId" },
                unique: true,
                filter: "\"EventType\" = 'BookingReminder'");
            migrationBuilder.DropTable(
                name: "WorkspaceApplicantProfiles");

            migrationBuilder.DropTable(
                name: "WorkspaceApplicationStatusHistory");

            migrationBuilder.DropColumn(
                name: "Availability",
                table: "WorkspaceApplications");

            migrationBuilder.DropColumn(
                name: "ProfileSummary",
                table: "WorkspaceApplications");

            migrationBuilder.DropColumn(
                name: "Skills",
                table: "WorkspaceApplications");
        }
    }
}
