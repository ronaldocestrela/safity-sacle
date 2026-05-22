using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafetyScale.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SectorDailyWorkloadAndScheduleSector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM ScheduleItems;");
            migrationBuilder.Sql("DELETE FROM MonthlySchedules;");

            migrationBuilder.AddColumn<int>(
                name: "RequiredGuardsPerDay",
                table: "Sectors",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "SectorId",
                table: "ScheduleItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleItems_SectorId",
                table: "ScheduleItems",
                column: "SectorId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleItems_TenantId_SectorId_Date",
                table: "ScheduleItems",
                columns: new[] { "TenantId", "SectorId", "Date" });

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleItems_Sectors_SectorId",
                table: "ScheduleItems",
                column: "SectorId",
                principalTable: "Sectors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleItems_Sectors_SectorId",
                table: "ScheduleItems");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleItems_SectorId",
                table: "ScheduleItems");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleItems_TenantId_SectorId_Date",
                table: "ScheduleItems");

            migrationBuilder.DropColumn(
                name: "RequiredGuardsPerDay",
                table: "Sectors");

            migrationBuilder.DropColumn(
                name: "SectorId",
                table: "ScheduleItems");
        }
    }
}
