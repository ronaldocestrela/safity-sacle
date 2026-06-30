using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafetyScale.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformPlansAndTenantLeadStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LeadStatus",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "PlatformPlanId",
                table: "Tenants",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlatformPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PriceMonthly = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_PlatformPlanId",
                table: "Tenants",
                column: "PlatformPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformPlans_Code",
                table: "PlatformPlans",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_PlatformPlans_PlatformPlanId",
                table: "Tenants",
                column: "PlatformPlanId",
                principalTable: "PlatformPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_PlatformPlans_PlatformPlanId",
                table: "Tenants");

            migrationBuilder.DropTable(
                name: "PlatformPlans");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_PlatformPlanId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LeadStatus",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PlatformPlanId",
                table: "Tenants");
        }
    }
}
