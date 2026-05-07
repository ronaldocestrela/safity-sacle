using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafetyScale.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueMonthlyScheduleMonthYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MonthlySchedules_Month_Year",
                table: "MonthlySchedules");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlySchedules_Month_Year",
                table: "MonthlySchedules",
                columns: new[] { "Month", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MonthlySchedules_Month_Year",
                table: "MonthlySchedules");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlySchedules_Month_Year",
                table: "MonthlySchedules",
                columns: new[] { "Month", "Year" });
        }
    }
}
