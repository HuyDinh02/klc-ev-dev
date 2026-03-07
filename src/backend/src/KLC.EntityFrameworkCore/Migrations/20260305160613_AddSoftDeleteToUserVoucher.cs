using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KLC.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToUserVoucher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AppUserVouchers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AppUserVouchers");
        }
    }
}
