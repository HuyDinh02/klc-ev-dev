using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KLC.Migrations
{
    /// <inheritdoc />
    public partial class AddTouPricingToTariffPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NormalRatePerKwh",
                table: "AppTariffPlans",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OffPeakRatePerKwh",
                table: "AppTariffPlans",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PeakRatePerKwh",
                table: "AppTariffPlans",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TariffType",
                table: "AppTariffPlans",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NormalRatePerKwh",
                table: "AppTariffPlans");

            migrationBuilder.DropColumn(
                name: "OffPeakRatePerKwh",
                table: "AppTariffPlans");

            migrationBuilder.DropColumn(
                name: "PeakRatePerKwh",
                table: "AppTariffPlans");

            migrationBuilder.DropColumn(
                name: "TariffType",
                table: "AppTariffPlans");
        }
    }
}
