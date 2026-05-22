using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafetyScale.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSectorsAndSecurityGuardSectors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sectors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sectors_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecurityGuardSectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SecurityGuardId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SectorId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityGuardSectors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityGuardSectors_Sectors_SectorId",
                        column: x => x.SectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SecurityGuardSectors_SecurityGuards_SecurityGuardId",
                        column: x => x.SecurityGuardId,
                        principalTable: "SecurityGuards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SecurityGuardSectors_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_TenantId_Name",
                table: "Sectors",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityGuardSectors_SectorId",
                table: "SecurityGuardSectors",
                column: "SectorId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityGuardSectors_SecurityGuardId",
                table: "SecurityGuardSectors",
                column: "SecurityGuardId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityGuardSectors_TenantId_SecurityGuardId_SectorId",
                table: "SecurityGuardSectors",
                columns: new[] { "TenantId", "SecurityGuardId", "SectorId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecurityGuardSectors");

            migrationBuilder.DropTable(
                name: "Sectors");
        }
    }
}
