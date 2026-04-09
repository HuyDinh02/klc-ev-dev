using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KLC.Migrations
{
    /// <inheritdoc />
    public partial class AddXminDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: xmin is a PostgreSQL system column — ALTER COLUMN is not supported on it.
            // HasDefaultValue(0u) in the EF Core model config exists only to satisfy SQLite
            // (used in tests), which has no automatic xmin system value. PostgreSQL manages
            // xmin internally and does not need a DEFAULT constraint.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: see Up().
        }
    }
}
