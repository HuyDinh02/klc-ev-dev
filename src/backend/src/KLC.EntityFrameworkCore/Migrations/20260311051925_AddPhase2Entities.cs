using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KLC.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase2Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MeteringClass",
                table: "AppConnectors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AppFleetAllowedStations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FleetId = table.Column<Guid>(type: "uuid", nullable: false),
                    StationGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppFleetAllowedStations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppFleetChargingSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FleetId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTimeUtc = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTimeUtc = table.Column<TimeSpan>(type: "interval", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppFleetChargingSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppFleets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OperatorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MaxMonthlyBudgetVnd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentMonthSpentVnd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ChargingPolicy = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    BudgetAlertThresholdPercent = table.Column<int>(type: "integer", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppFleets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppOperators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ApiKeyHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WebhookUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RateLimitPerMinute = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppOperators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppOperatorWebhookLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppOperatorWebhookLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppPowerSharingGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MaxCapacityKw = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    DistributionStrategy = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MinPowerPerConnectorKw = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    StationGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPowerSharingGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSiteLoadProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PowerSharingGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TotalLoadKw = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    AvailableCapacityKw = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ActiveSessionCount = table.Column<int>(type: "integer", nullable: false),
                    TotalConnectorCount = table.Column<int>(type: "integer", nullable: false),
                    PeakLoadKw = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSiteLoadProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppFleetVehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FleetId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DailyChargingLimitKwh = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrentDayEnergyKwh = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentMonthEnergyKwh = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppFleetVehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppFleetVehicles_AppFleets_FleetId",
                        column: x => x.FleetId,
                        principalTable: "AppFleets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppOperatorStations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    StationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppOperatorStations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppOperatorStations_AppOperators_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "AppOperators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppPowerSharingGroupMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PowerSharingGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    StationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    AllocatedPowerKw = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPowerSharingGroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPowerSharingGroupMembers_AppPowerSharingGroups_PowerShar~",
                        column: x => x.PowerSharingGroupId,
                        principalTable: "AppPowerSharingGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppFleetAllowedStations_FleetId",
                table: "AppFleetAllowedStations",
                column: "FleetId");

            migrationBuilder.CreateIndex(
                name: "IX_AppFleetAllowedStations_FleetId_StationGroupId",
                table: "AppFleetAllowedStations",
                columns: new[] { "FleetId", "StationGroupId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppFleetAllowedStations_StationGroupId",
                table: "AppFleetAllowedStations",
                column: "StationGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AppFleetChargingSchedules_FleetId",
                table: "AppFleetChargingSchedules",
                column: "FleetId");

            migrationBuilder.CreateIndex(
                name: "IX_AppFleetChargingSchedules_FleetId_DayOfWeek",
                table: "AppFleetChargingSchedules",
                columns: new[] { "FleetId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_AppFleets_IsActive",
                table: "AppFleets",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppFleets_Name",
                table: "AppFleets",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_AppFleets_OperatorUserId",
                table: "AppFleets",
                column: "OperatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppFleetVehicles_DriverUserId",
                table: "AppFleetVehicles",
                column: "DriverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppFleetVehicles_FleetId",
                table: "AppFleetVehicles",
                column: "FleetId");

            migrationBuilder.CreateIndex(
                name: "IX_AppFleetVehicles_FleetId_VehicleId",
                table: "AppFleetVehicles",
                columns: new[] { "FleetId", "VehicleId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppFleetVehicles_IsActive",
                table: "AppFleetVehicles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppFleetVehicles_VehicleId",
                table: "AppFleetVehicles",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_AppOperators_ApiKeyHash",
                table: "AppOperators",
                column: "ApiKeyHash");

            migrationBuilder.CreateIndex(
                name: "IX_AppOperators_IsActive",
                table: "AppOperators",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppOperators_Name",
                table: "AppOperators",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppOperatorStations_OperatorId",
                table: "AppOperatorStations",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppOperatorStations_OperatorId_StationId",
                table: "AppOperatorStations",
                columns: new[] { "OperatorId", "StationId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppOperatorStations_StationId",
                table: "AppOperatorStations",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_AppOperatorWebhookLogs_CreationTime",
                table: "AppOperatorWebhookLogs",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppOperatorWebhookLogs_EventType",
                table: "AppOperatorWebhookLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AppOperatorWebhookLogs_OperatorId",
                table: "AppOperatorWebhookLogs",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPowerSharingGroupMembers_ConnectorId",
                table: "AppPowerSharingGroupMembers",
                column: "ConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPowerSharingGroupMembers_PowerSharingGroupId",
                table: "AppPowerSharingGroupMembers",
                column: "PowerSharingGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPowerSharingGroupMembers_PowerSharingGroupId_ConnectorId",
                table: "AppPowerSharingGroupMembers",
                columns: new[] { "PowerSharingGroupId", "ConnectorId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppPowerSharingGroupMembers_StationId",
                table: "AppPowerSharingGroupMembers",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPowerSharingGroups_IsActive",
                table: "AppPowerSharingGroups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppPowerSharingGroups_Mode",
                table: "AppPowerSharingGroups",
                column: "Mode");

            migrationBuilder.CreateIndex(
                name: "IX_AppPowerSharingGroups_StationGroupId",
                table: "AppPowerSharingGroups",
                column: "StationGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSiteLoadProfiles_PowerSharingGroupId",
                table: "AppSiteLoadProfiles",
                column: "PowerSharingGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSiteLoadProfiles_PowerSharingGroupId_Timestamp",
                table: "AppSiteLoadProfiles",
                columns: new[] { "PowerSharingGroupId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AppSiteLoadProfiles_Timestamp",
                table: "AppSiteLoadProfiles",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppFleetAllowedStations");

            migrationBuilder.DropTable(
                name: "AppFleetChargingSchedules");

            migrationBuilder.DropTable(
                name: "AppFleetVehicles");

            migrationBuilder.DropTable(
                name: "AppOperatorStations");

            migrationBuilder.DropTable(
                name: "AppOperatorWebhookLogs");

            migrationBuilder.DropTable(
                name: "AppPowerSharingGroupMembers");

            migrationBuilder.DropTable(
                name: "AppSiteLoadProfiles");

            migrationBuilder.DropTable(
                name: "AppFleets");

            migrationBuilder.DropTable(
                name: "AppOperators");

            migrationBuilder.DropTable(
                name: "AppPowerSharingGroups");

            migrationBuilder.DropColumn(
                name: "MeteringClass",
                table: "AppConnectors");
        }
    }
}
