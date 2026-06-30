using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafetyScale.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformPlanLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxSectors",
                table: "PlatformPlans",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "MaxSecurityGuards",
                table: "PlatformPlans",
                type: "int",
                nullable: false,
                defaultValue: 10);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxSectors",
                table: "PlatformPlans");

            migrationBuilder.DropColumn(
                name: "MaxSecurityGuards",
                table: "PlatformPlans");
        }
    }
}
