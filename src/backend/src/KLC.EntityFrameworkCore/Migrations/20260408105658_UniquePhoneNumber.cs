using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KLC.Migrations
{
    /// <inheritdoc />
    public partial class UniquePhoneNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppAppUsers_PhoneNumber",
                table: "AppAppUsers");

            migrationBuilder.CreateIndex(
                name: "IX_AppAppUsers_PhoneNumber",
                table: "AppAppUsers",
                column: "PhoneNumber",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppAppUsers_PhoneNumber",
                table: "AppAppUsers");

            migrationBuilder.CreateIndex(
                name: "IX_AppAppUsers_PhoneNumber",
                table: "AppAppUsers",
                column: "PhoneNumber");
        }
    }
}
