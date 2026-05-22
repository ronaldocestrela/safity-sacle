using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafetyScale.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantLogicalIsolation : Migration
    {
        /// <summary>Stable ID for the default tenant row required before FK constraints are applied.</summary>
        private static readonly Guid DefaultTenantId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnavailableDays_SecurityGuardId_Date",
                table: "UnavailableDays");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleItems_SecurityGuardId_Date",
                table: "ScheduleItems");

            migrationBuilder.DropIndex(
                name: "IX_MonthlySchedules_Month_Year",
                table: "MonthlySchedules");

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "Name", "Slug", "IsActive", "CreatedAt" },
                values: new object[]
                {
                    DefaultTenantId,
                    "Default",
                    "default",
                    true,
                    new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                });

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "UnavailableDays",
                type: "TEXT",
                nullable: false,
                defaultValue: DefaultTenantId);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "SecurityGuards",
                type: "TEXT",
                nullable: false,
                defaultValue: DefaultTenantId);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ScheduleItems",
                type: "TEXT",
                nullable: false,
                defaultValue: DefaultTenantId);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "MonthlySchedules",
                type: "TEXT",
                nullable: false,
                defaultValue: DefaultTenantId);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: DefaultTenantId);

            migrationBuilder.CreateIndex(
                name: "IX_UnavailableDays_SecurityGuardId",
                table: "UnavailableDays",
                column: "SecurityGuardId");

            migrationBuilder.CreateIndex(
                name: "IX_UnavailableDays_TenantId_SecurityGuardId_Date",
                table: "UnavailableDays",
                columns: new[] { "TenantId", "SecurityGuardId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityGuards_TenantId",
                table: "SecurityGuards",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleItems_SecurityGuardId",
                table: "ScheduleItems",
                column: "SecurityGuardId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleItems_TenantId_SecurityGuardId_Date",
                table: "ScheduleItems",
                columns: new[] { "TenantId", "SecurityGuardId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlySchedules_TenantId_Month_Year",
                table: "MonthlySchedules",
                columns: new[] { "TenantId", "Month", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TenantId",
                table: "AspNetUsers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Tenants_TenantId",
                table: "AspNetUsers",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlySchedules_Tenants_TenantId",
                table: "MonthlySchedules",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleItems_Tenants_TenantId",
                table: "ScheduleItems",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SecurityGuards_Tenants_TenantId",
                table: "SecurityGuards",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UnavailableDays_Tenants_TenantId",
                table: "UnavailableDays",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Tenants_TenantId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_MonthlySchedules_Tenants_TenantId",
                table: "MonthlySchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleItems_Tenants_TenantId",
                table: "ScheduleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SecurityGuards_Tenants_TenantId",
                table: "SecurityGuards");

            migrationBuilder.DropForeignKey(
                name: "FK_UnavailableDays_Tenants_TenantId",
                table: "UnavailableDays");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_UnavailableDays_SecurityGuardId",
                table: "UnavailableDays");

            migrationBuilder.DropIndex(
                name: "IX_UnavailableDays_TenantId_SecurityGuardId_Date",
                table: "UnavailableDays");

            migrationBuilder.DropIndex(
                name: "IX_SecurityGuards_TenantId",
                table: "SecurityGuards");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleItems_SecurityGuardId",
                table: "ScheduleItems");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleItems_TenantId_SecurityGuardId_Date",
                table: "ScheduleItems");

            migrationBuilder.DropIndex(
                name: "IX_MonthlySchedules_TenantId_Month_Year",
                table: "MonthlySchedules");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TenantId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "UnavailableDays");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SecurityGuards");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ScheduleItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MonthlySchedules");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_UnavailableDays_SecurityGuardId_Date",
                table: "UnavailableDays",
                columns: new[] { "SecurityGuardId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleItems_SecurityGuardId_Date",
                table: "ScheduleItems",
                columns: new[] { "SecurityGuardId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlySchedules_Month_Year",
                table: "MonthlySchedules",
                columns: new[] { "Month", "Year" },
                unique: true);
        }
    }
}
