using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KLC.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectorQrCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QrCodeData",
                table: "AppConnectors",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QrCodeData",
                table: "AppConnectors");
        }
    }
}
