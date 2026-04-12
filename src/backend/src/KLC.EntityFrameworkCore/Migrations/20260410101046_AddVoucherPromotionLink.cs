using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KLC.Migrations
{
    /// <inheritdoc />
    public partial class AddVoucherPromotionLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PromotionId",
                table: "AppVouchers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppVouchers_PromotionId",
                table: "AppVouchers",
                column: "PromotionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppVouchers_PromotionId",
                table: "AppVouchers");

            migrationBuilder.DropColumn(
                name: "PromotionId",
                table: "AppVouchers");
        }
    }
}
