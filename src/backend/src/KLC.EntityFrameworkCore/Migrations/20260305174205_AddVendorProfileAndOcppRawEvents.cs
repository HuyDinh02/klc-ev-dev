using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KLC.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorProfileAndOcppRawEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VendorProfile",
                table: "AppChargingStations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AppOcppRawEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargePointId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UniqueId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MessageType = table.Column<int>(type: "integer", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    LatencyMs = table.Column<long>(type: "bigint", nullable: true),
                    VendorProfile = table.Column<int>(type: "integer", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppOcppRawEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppOcppRawEvents_Action",
                table: "AppOcppRawEvents",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AppOcppRawEvents_ChargePointId",
                table: "AppOcppRawEvents",
                column: "ChargePointId");

            migrationBuilder.CreateIndex(
                name: "IX_AppOcppRawEvents_ChargePointId_ReceivedAt",
                table: "AppOcppRawEvents",
                columns: new[] { "ChargePointId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AppOcppRawEvents_ReceivedAt",
                table: "AppOcppRawEvents",
                column: "ReceivedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppOcppRawEvents");

            migrationBuilder.DropColumn(
                name: "VendorProfile",
                table: "AppChargingStations");
        }
    }
}
