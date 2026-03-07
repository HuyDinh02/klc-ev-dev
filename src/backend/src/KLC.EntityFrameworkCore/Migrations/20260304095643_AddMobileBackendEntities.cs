using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KLC.Migrations
{
    /// <inheritdoc />
    public partial class AddMobileBackendEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "AppAppUsers",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "AppAppUsers",
                type: "character varying(1)",
                maxLength: 1,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastTopUpAt",
                table: "AppAppUsers",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MembershipTier",
                table: "AppAppUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AppDeviceTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Platform = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppDeviceTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppFavoriteStations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppFavoriteStations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppNotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargingComplete = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentAlerts = table.Column<bool>(type: "boolean", nullable: false),
                    FaultAlerts = table.Column<bool>(type: "boolean", nullable: false),
                    Promotions = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppNotificationPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppPromotions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_AppPromotions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppStationAmenities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmenityType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppStationAmenities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppStationAmenities_AppChargingStations_StationId",
                        column: x => x.StationId,
                        principalTable: "AppChargingStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppStationPhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppStationPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppStationPhotos_AppChargingStations_StationId",
                        column: x => x.StationId,
                        principalTable: "AppChargingStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppUserFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AdminResponse = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RespondedBy = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_AppUserFeedbacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppUserVouchers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoucherId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserVouchers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppVouchers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: true),
                    MaxDiscountAmount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TotalQuantity = table.Column<int>(type: "integer", nullable: false),
                    UsedQuantity = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_AppVouchers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppWalletTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    PaymentGateway = table.Column<int>(type: "integer", nullable: true),
                    GatewayTransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RelatedSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReferenceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppWalletTransactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppDeviceTokens_IsActive",
                table: "AppDeviceTokens",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppDeviceTokens_Token",
                table: "AppDeviceTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppDeviceTokens_UserId",
                table: "AppDeviceTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppFavoriteStations_UserId",
                table: "AppFavoriteStations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppFavoriteStations_UserId_StationId",
                table: "AppFavoriteStations",
                columns: new[] { "UserId", "StationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppNotificationPreferences_UserId",
                table: "AppNotificationPreferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppPromotions_EndDate",
                table: "AppPromotions",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppPromotions_IsActive",
                table: "AppPromotions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppPromotions_StartDate",
                table: "AppPromotions",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppPromotions_Type",
                table: "AppPromotions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_AppStationAmenities_StationId",
                table: "AppStationAmenities",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_AppStationAmenities_StationId_AmenityType",
                table: "AppStationAmenities",
                columns: new[] { "StationId", "AmenityType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppStationPhotos_StationId",
                table: "AppStationPhotos",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_AppStationPhotos_StationId_IsPrimary",
                table: "AppStationPhotos",
                columns: new[] { "StationId", "IsPrimary" },
                filter: "\"IsPrimary\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserFeedbacks_CreationTime",
                table: "AppUserFeedbacks",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserFeedbacks_Status",
                table: "AppUserFeedbacks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserFeedbacks_Type",
                table: "AppUserFeedbacks",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserFeedbacks_UserId",
                table: "AppUserFeedbacks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserVouchers_UserId",
                table: "AppUserVouchers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserVouchers_UserId_VoucherId",
                table: "AppUserVouchers",
                columns: new[] { "UserId", "VoucherId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUserVouchers_VoucherId",
                table: "AppUserVouchers",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_AppVouchers_Code",
                table: "AppVouchers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppVouchers_ExpiryDate",
                table: "AppVouchers",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppVouchers_IsActive",
                table: "AppVouchers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppWalletTransactions_CreationTime",
                table: "AppWalletTransactions",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppWalletTransactions_ReferenceCode",
                table: "AppWalletTransactions",
                column: "ReferenceCode");

            migrationBuilder.CreateIndex(
                name: "IX_AppWalletTransactions_RelatedSessionId",
                table: "AppWalletTransactions",
                column: "RelatedSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppWalletTransactions_Status",
                table: "AppWalletTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppWalletTransactions_Type",
                table: "AppWalletTransactions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_AppWalletTransactions_UserId",
                table: "AppWalletTransactions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppDeviceTokens");

            migrationBuilder.DropTable(
                name: "AppFavoriteStations");

            migrationBuilder.DropTable(
                name: "AppNotificationPreferences");

            migrationBuilder.DropTable(
                name: "AppPromotions");

            migrationBuilder.DropTable(
                name: "AppStationAmenities");

            migrationBuilder.DropTable(
                name: "AppStationPhotos");

            migrationBuilder.DropTable(
                name: "AppUserFeedbacks");

            migrationBuilder.DropTable(
                name: "AppUserVouchers");

            migrationBuilder.DropTable(
                name: "AppVouchers");

            migrationBuilder.DropTable(
                name: "AppWalletTransactions");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "AppAppUsers");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "AppAppUsers");

            migrationBuilder.DropColumn(
                name: "LastTopUpAt",
                table: "AppAppUsers");

            migrationBuilder.DropColumn(
                name: "MembershipTier",
                table: "AppAppUsers");
        }
    }
}
