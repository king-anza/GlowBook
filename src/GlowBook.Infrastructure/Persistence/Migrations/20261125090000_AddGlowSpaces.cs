using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GlowBook.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GlowBookDbContext))]
[Migration("20261125090000_AddGlowSpaces")]
public sealed class AddGlowSpaces : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "WorkspaceOpportunities",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                WorkspaceType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                OpportunityType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                RequiredSkills = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                IncludedAmenities = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                HouseRules = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                SpacesAvailable = table.Column<int>(type: "integer", nullable: false),
                RateAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                RatePeriod = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                Recurring = table.Column<bool>(type: "boolean", nullable: false),
                BringOwnClients = table.Column<bool>(type: "boolean", nullable: false),
                ApprovalRequired = table.Column<bool>(type: "boolean", nullable: false),
                ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WorkspaceOpportunities", x => x.Id);
                table.CheckConstraint("CK_WorkspaceOpportunity_Dates", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
                table.CheckConstraint("CK_WorkspaceOpportunity_Rate", "\"RateAmount\" IS NULL OR \"RateAmount\" >= 0");
                table.CheckConstraint("CK_WorkspaceOpportunity_Spaces", "\"SpacesAvailable\" BETWEEN 1 AND 50");
                table.ForeignKey("FK_WorkspaceOpportunities_Businesses_BusinessId", x => x.BusinessId, "Businesses", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "WorkspaceApplications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OpportunityId = table.Column<Guid>(type: "uuid", nullable: false),
                ApplicantUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Message = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                PortfolioUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WorkspaceApplications", x => x.Id);
                table.ForeignKey("FK_WorkspaceApplications_AspNetUsers_ApplicantUserId", x => x.ApplicantUserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_WorkspaceApplications_WorkspaceOpportunities_OpportunityId", x => x.OpportunityId, "WorkspaceOpportunities", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_WorkspaceApplications_ApplicantUserId", "WorkspaceApplications", "ApplicantUserId");
        migrationBuilder.CreateIndex("IX_WorkspaceApplications_OpportunityId_ApplicantUserId", "WorkspaceApplications", new[] { "OpportunityId", "ApplicantUserId" }, unique: true);
        migrationBuilder.CreateIndex("IX_WorkspaceOpportunities_BusinessId", "WorkspaceOpportunities", "BusinessId");
        migrationBuilder.CreateIndex("IX_WorkspaceOpportunities_Status_StartDate", "WorkspaceOpportunities", new[] { "Status", "StartDate" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("WorkspaceApplications");
        migrationBuilder.DropTable("WorkspaceOpportunities");
    }
}
