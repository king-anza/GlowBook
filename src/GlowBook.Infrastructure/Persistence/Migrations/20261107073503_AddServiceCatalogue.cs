using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GlowBook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceCatalogue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Services_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Services_ServiceCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ServiceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceOfferings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    BufferMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOfferings", x => x.Id);
                    table.CheckConstraint("CK_ServiceOffering_Buffer", "\"BufferMinutes\" BETWEEN 0 AND 180 AND \"BufferMinutes\" % 5 = 0");
                    table.CheckConstraint("CK_ServiceOffering_Duration", "\"DurationMinutes\" BETWEEN 5 AND 720 AND \"DurationMinutes\" % 5 = 0");
                    table.CheckConstraint("CK_ServiceOffering_Price", "\"Price\" >= 0 AND \"Price\" <= 999999.99");
                    table.ForeignKey(
                        name: "FK_ServiceOfferings_BusinessLocations_BusinessLocationId",
                        column: x => x.BusinessLocationId,
                        principalTable: "BusinessLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceOfferings_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceOfferings_StaffMembers_StaffMemberId",
                        column: x => x.StaffMemberId,
                        principalTable: "StaffMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ServiceCategories",
                columns: new[] { "Id", "Description", "DisplayOrder", "IsActive", "Name", "Slug" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-000000000001"), "", 1, true, "Hair", "hair" },
                    { new Guid("44444444-4444-4444-4444-000000000002"), "", 2, true, "Barbering", "barbering" },
                    { new Guid("44444444-4444-4444-4444-000000000003"), "", 3, true, "Nails", "nails" },
                    { new Guid("44444444-4444-4444-4444-000000000004"), "", 4, true, "Makeup", "makeup" },
                    { new Guid("44444444-4444-4444-4444-000000000005"), "", 5, true, "Skincare and Facials", "skincare-facials" },
                    { new Guid("44444444-4444-4444-4444-000000000006"), "", 6, true, "Eyebrows and Lashes", "eyebrows-lashes" },
                    { new Guid("44444444-4444-4444-4444-000000000007"), "", 7, true, "Hair Removal", "hair-removal" },
                    { new Guid("44444444-4444-4444-4444-000000000008"), "", 8, true, "Massage and Body Treatments", "massage-body-treatments" },
                    { new Guid("44444444-4444-4444-4444-000000000009"), "", 9, true, "Bridal and Special Occasion", "bridal-special-occasion" },
                    { new Guid("44444444-4444-4444-4444-000000000010"), "", 10, true, "Other Beauty Services", "other-beauty-services" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCategories_Slug",
                table: "ServiceCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOfferings_BusinessLocationId",
                table: "ServiceOfferings",
                column: "BusinessLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOfferings_ServiceId_StaffMemberId_BusinessLocationId",
                table: "ServiceOfferings",
                columns: new[] { "ServiceId", "StaffMemberId", "BusinessLocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOfferings_StaffMemberId",
                table: "ServiceOfferings",
                column: "StaffMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_BusinessId_Name",
                table: "Services",
                columns: new[] { "BusinessId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Services_CategoryId",
                table: "Services",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceOfferings");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "ServiceCategories");
        }
    }
}
