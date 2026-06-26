using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafetyScale.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnsureAspNetUsersSecurityGuardIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('AspNetUsers', 'SecurityGuardId') IS NULL
                BEGIN
                    ALTER TABLE [AspNetUsers] ADD [SecurityGuardId] uniqueidentifier NULL;
                END

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_AspNetUsers_SecurityGuardId'
                      AND object_id = OBJECT_ID('AspNetUsers'))
                BEGIN
                    CREATE INDEX [IX_AspNetUsers_SecurityGuardId]
                        ON [AspNetUsers] ([SecurityGuardId]);
                END

                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_AspNetUsers_SecurityGuards_SecurityGuardId')
                BEGIN
                    ALTER TABLE [AspNetUsers] WITH CHECK
                        ADD CONSTRAINT [FK_AspNetUsers_SecurityGuards_SecurityGuardId]
                        FOREIGN KEY ([SecurityGuardId]) REFERENCES [SecurityGuards] ([Id]);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_AspNetUsers_SecurityGuards_SecurityGuardId')
                BEGIN
                    ALTER TABLE [AspNetUsers] DROP CONSTRAINT [FK_AspNetUsers_SecurityGuards_SecurityGuardId];
                END

                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_AspNetUsers_SecurityGuardId'
                      AND object_id = OBJECT_ID('AspNetUsers'))
                BEGIN
                    DROP INDEX [IX_AspNetUsers_SecurityGuardId] ON [AspNetUsers];
                END

                IF COL_LENGTH('AspNetUsers', 'SecurityGuardId') IS NOT NULL
                BEGIN
                    ALTER TABLE [AspNetUsers] DROP COLUMN [SecurityGuardId];
                END
                """);
        }
    }
}
