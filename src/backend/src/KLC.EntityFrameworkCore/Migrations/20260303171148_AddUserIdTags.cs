using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KLC.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUserIdTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdTag = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TagType = table.Column<int>(type: "integer", nullable: false),
                    FriendlyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
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
                    table.PrimaryKey("PK_AppUserIdTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppUserPaymentMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Gateway = table.Column<int>(type: "integer", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    TokenReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LastFourDigits = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
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
                    table.PrimaryKey("PK_AppUserPaymentMethods", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUserIdTags_IdTag",
                table: "AppUserIdTags",
                column: "IdTag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUserIdTags_IsActive",
                table: "AppUserIdTags",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserIdTags_UserId",
                table: "AppUserIdTags",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserPaymentMethods_IsActive",
                table: "AppUserPaymentMethods",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserPaymentMethods_UserId",
                table: "AppUserPaymentMethods",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserPaymentMethods_UserId_IsDefault",
                table: "AppUserPaymentMethods",
                columns: new[] { "UserId", "IsDefault" },
                filter: "\"IsDefault\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUserIdTags");

            migrationBuilder.DropTable(
                name: "AppUserPaymentMethods");
        }
    }
}
