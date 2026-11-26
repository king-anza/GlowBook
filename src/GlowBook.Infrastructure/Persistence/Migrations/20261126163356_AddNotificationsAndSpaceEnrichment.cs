using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GlowBook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsAndSpaceEnrichment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "ImageUrls",
                table: "WorkspaceOpportunities",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string>(
                name: "ActionUrl",
                table: "Notifications",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "Notifications",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PushDeliveredAtUtc",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PushProcessedAtUtc",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAtUtc",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Notifications",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "Notifications"
                SET "Title" = CASE WHEN "EventType" = 'BookingCreated' THEN 'New booking request' ELSE 'Booking update' END,
                    "Message" = 'A booking has changed. Open bookings to review the details.',
                    "ActionUrl" = '/bookings',
                    "PushProcessedAtUtc" = NOW()
                WHERE "SubjectType" = 'Booking';
                """);

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingPush = table.Column<bool>(type: "boolean", nullable: false),
                    ApplicationPush = table.Column<bool>(type: "boolean", nullable: false),
                    AccountPush = table.Column<bool>(type: "boolean", nullable: false),
                    ReminderPush = table.Column<bool>(type: "boolean", nullable: false),
                    EmailFallback = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PushDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProtectedToken = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: false),
                    Platform = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PushDevices_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedWorkspaceOpportunities",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpportunityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedWorkspaceOpportunities", x => new { x.UserId, x.OpportunityId });
                    table.ForeignKey(
                        name: "FK_SavedWorkspaceOpportunities_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedWorkspaceOpportunities_WorkspaceOpportunities_Opportun~",
                        column: x => x.OpportunityId,
                        principalTable: "WorkspaceOpportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceOpportunityReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OpportunityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceOpportunityReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceOpportunityReports_AspNetUsers_ReporterUserId",
                        column: x => x.ReporterUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkspaceOpportunityReports_WorkspaceOpportunities_Opportun~",
                        column: x => x.OpportunityId,
                        principalTable: "WorkspaceOpportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId_EventType_SubjectId",
                table: "Notifications",
                columns: new[] { "RecipientUserId", "EventType", "SubjectId" },
                unique: true,
                filter: "\"EventType\" = 'BookingReminder'");

            migrationBuilder.CreateIndex(
                name: "IX_PushDevices_TokenHash",
                table: "PushDevices",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushDevices_UserId_RevokedAtUtc",
                table: "PushDevices",
                columns: new[] { "UserId", "RevokedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedWorkspaceOpportunities_OpportunityId",
                table: "SavedWorkspaceOpportunities",
                column: "OpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedWorkspaceOpportunities_UserId_CreatedAtUtc",
                table: "SavedWorkspaceOpportunities",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceOpportunityReports_OpportunityId_ReporterUserId",
                table: "WorkspaceOpportunityReports",
                columns: new[] { "OpportunityId", "ReporterUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceOpportunityReports_ReporterUserId",
                table: "WorkspaceOpportunityReports",
                column: "ReporterUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "PushDevices");

            migrationBuilder.DropTable(
                name: "SavedWorkspaceOpportunities");

            migrationBuilder.DropTable(
                name: "WorkspaceOpportunityReports");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipientUserId_EventType_SubjectId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ImageUrls",
                table: "WorkspaceOpportunities");

            migrationBuilder.DropColumn(
                name: "ActionUrl",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Message",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "PushDeliveredAtUtc",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "PushProcessedAtUtc",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ReadAtUtc",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Notifications");
        }
    }
}
