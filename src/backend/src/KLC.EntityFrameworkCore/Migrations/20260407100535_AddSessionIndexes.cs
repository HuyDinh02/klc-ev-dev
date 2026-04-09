using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KLC.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppMeterValues_SessionId",
                table: "AppMeterValues");

            migrationBuilder.DropIndex(
                name: "IX_AppChargingSessions_UserId",
                table: "AppChargingSessions");

            migrationBuilder.CreateIndex(
                name: "IX_AppMeterValues_SessionId_Timestamp",
                table: "AppMeterValues",
                columns: new[] { "SessionId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AppChargingSessions_UserId_Active",
                table: "AppChargingSessions",
                column: "UserId",
                unique: true,
                filter: "\"Status\" IN (0, 1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_AppChargingSessions_UserId_Status",
                table: "AppChargingSessions",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppMeterValues_SessionId_Timestamp",
                table: "AppMeterValues");

            migrationBuilder.DropIndex(
                name: "IX_AppChargingSessions_UserId_Active",
                table: "AppChargingSessions");

            migrationBuilder.DropIndex(
                name: "IX_AppChargingSessions_UserId_Status",
                table: "AppChargingSessions");

            migrationBuilder.CreateIndex(
                name: "IX_AppMeterValues_SessionId",
                table: "AppMeterValues",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppChargingSessions_UserId",
                table: "AppChargingSessions",
                column: "UserId");
        }
    }
}
