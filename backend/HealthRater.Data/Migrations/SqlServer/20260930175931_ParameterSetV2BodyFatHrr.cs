using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthRater.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class ParameterSetV2BodyFatHrr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing rows were all saved before this change: their body fat was entered by the
            // user (Measured) and they were scored with the 39-parameter set (v1-39, max 390).
            // Their stored scores are kept as-is; new rows get "v2-41" from the application.
            migrationBuilder.AddColumn<string>(
                name: "BodyFatEstimationMethod",
                table: "HealthAssessments",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyFatSource",
                table: "HealthAssessments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Measured");

            migrationBuilder.AddColumn<int>(
                name: "HeartRateAfter60Seconds",
                table: "HealthAssessments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParameterSetVersion",
                table: "HealthAssessments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "v1-39");

            migrationBuilder.AddColumn<int>(
                name: "PeakHeartRate",
                table: "HealthAssessments",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BodyFatEstimationMethod",
                table: "HealthAssessments");

            migrationBuilder.DropColumn(
                name: "BodyFatSource",
                table: "HealthAssessments");

            migrationBuilder.DropColumn(
                name: "HeartRateAfter60Seconds",
                table: "HealthAssessments");

            migrationBuilder.DropColumn(
                name: "ParameterSetVersion",
                table: "HealthAssessments");

            migrationBuilder.DropColumn(
                name: "PeakHeartRate",
                table: "HealthAssessments");
        }
    }
}
