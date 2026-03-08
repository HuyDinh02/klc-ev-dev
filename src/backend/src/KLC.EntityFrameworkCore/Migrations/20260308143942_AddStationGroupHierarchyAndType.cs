using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KLC.Migrations
{
    /// <inheritdoc />
    public partial class AddStationGroupHierarchyAndType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GroupType",
                table: "AppStationGroups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentGroupId",
                table: "AppStationGroups",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppStationGroups_GroupType",
                table: "AppStationGroups",
                column: "GroupType");

            migrationBuilder.CreateIndex(
                name: "IX_AppStationGroups_ParentGroupId",
                table: "AppStationGroups",
                column: "ParentGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppStationGroups_AppStationGroups_ParentGroupId",
                table: "AppStationGroups",
                column: "ParentGroupId",
                principalTable: "AppStationGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppStationGroups_AppStationGroups_ParentGroupId",
                table: "AppStationGroups");

            migrationBuilder.DropIndex(
                name: "IX_AppStationGroups_GroupType",
                table: "AppStationGroups");

            migrationBuilder.DropIndex(
                name: "IX_AppStationGroups_ParentGroupId",
                table: "AppStationGroups");

            migrationBuilder.DropColumn(
                name: "GroupType",
                table: "AppStationGroups");

            migrationBuilder.DropColumn(
                name: "ParentGroupId",
                table: "AppStationGroups");
        }
    }
}
