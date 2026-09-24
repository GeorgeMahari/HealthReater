using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthRater.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HealthAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScoringVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TotalHealthRating = table.Column<int>(type: "int", nullable: false),
                    TotalPossibleScore = table.Column<int>(type: "int", nullable: false),
                    Percentage = table.Column<double>(type: "float", nullable: false),
                    EnergyStrengthStaminaRaw = table.Column<int>(type: "int", nullable: false),
                    EnergyStrengthStaminaMax = table.Column<int>(type: "int", nullable: false),
                    EnergyStrengthStaminaScore = table.Column<double>(type: "float", nullable: false),
                    MentalEmotionalRaw = table.Column<int>(type: "int", nullable: false),
                    MentalEmotionalMax = table.Column<int>(type: "int", nullable: false),
                    MentalEmotionalScore = table.Column<double>(type: "float", nullable: false),
                    ImmunityRaw = table.Column<int>(type: "int", nullable: false),
                    ImmunityMax = table.Column<int>(type: "int", nullable: false),
                    ImmunityScore = table.Column<double>(type: "float", nullable: false),
                    LongevityRaw = table.Column<int>(type: "int", nullable: false),
                    LongevityMax = table.Column<int>(type: "int", nullable: false),
                    LongevityScore = table.Column<double>(type: "float", nullable: false),
                    Bmi = table.Column<double>(type: "float", nullable: false),
                    WaistToHeightRatio = table.Column<double>(type: "float", nullable: false),
                    WaistToHipRatio = table.Column<double>(type: "float", nullable: false),
                    RestingHeartRate = table.Column<int>(type: "int", nullable: false),
                    HeartRateRecovery = table.Column<int>(type: "int", nullable: false),
                    BloodPressureSystolic = table.Column<int>(type: "int", nullable: false),
                    BloodPressureDiastolic = table.Column<int>(type: "int", nullable: false),
                    Sex = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AgeAtAssessment = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<double>(type: "float", nullable: false),
                    HeightUnit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    WeightUnit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Waist = table.Column<double>(type: "float", nullable: false),
                    WaistUnit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Hip = table.Column<double>(type: "float", nullable: false),
                    HipUnit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    BodyFatPercentage = table.Column<double>(type: "float", nullable: false),
                    InputSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthAssessments", x => x.Id);
                    table.CheckConstraint("CK_HealthAssessments_Age", "AgeAtAssessment BETWEEN 18 AND 100");
                    table.CheckConstraint("CK_HealthAssessments_Total", "TotalHealthRating >= 0 AND TotalHealthRating <= TotalPossibleScore");
                    table.ForeignKey(
                        name: "FK_HealthAssessments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentParameterScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParameterKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ParameterName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    RawValue = table.Column<double>(type: "float", nullable: true),
                    RawText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NormalizedValue = table.Column<double>(type: "float", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Score = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentParameterScores", x => x.Id);
                    table.CheckConstraint("CK_AssessmentParameterScores_Score", "Score BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "FK_AssessmentParameterScores_HealthAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "HealthAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentParameterScores_AssessmentId",
                table: "AssessmentParameterScores",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentParameterScores_AssessmentId_ParameterKey",
                table: "AssessmentParameterScores",
                columns: new[] { "AssessmentId", "ParameterKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HealthAssessments_CompletedAt",
                table: "HealthAssessments",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_HealthAssessments_UserId",
                table: "HealthAssessments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthAssessments_UserId_Status_CompletedAt",
                table: "HealthAssessments",
                columns: new[] { "UserId", "Status", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentParameterScores");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "HealthAssessments");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
