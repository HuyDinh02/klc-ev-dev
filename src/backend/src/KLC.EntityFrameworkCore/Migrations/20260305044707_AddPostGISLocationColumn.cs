using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace KLC.Migrations
{
    /// <inheritdoc />
    public partial class AddPostGISLocationColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "AppChargingStations",
                type: "geography (point, 4326)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppChargingStations_Location",
                table: "AppChargingStations",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            // Populate Location from existing Latitude/Longitude values
            migrationBuilder.Sql(
                """
                UPDATE "AppChargingStations"
                SET "Location" = ST_SetSRID(ST_MakePoint("Longitude", "Latitude"), 4326)::geography
                WHERE "Latitude" != 0 AND "Longitude" != 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppChargingStations_Location",
                table: "AppChargingStations");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "AppChargingStations");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");
        }
    }
}
