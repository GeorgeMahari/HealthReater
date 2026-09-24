using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthRater.Data.Migrations.Sqlite
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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    AvatarUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HealthAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ScoringVersion = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    TotalHealthRating = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalPossibleScore = table.Column<int>(type: "INTEGER", nullable: false),
                    Percentage = table.Column<double>(type: "REAL", nullable: false),
                    EnergyStrengthStaminaRaw = table.Column<int>(type: "INTEGER", nullable: false),
                    EnergyStrengthStaminaMax = table.Column<int>(type: "INTEGER", nullable: false),
                    EnergyStrengthStaminaScore = table.Column<double>(type: "REAL", nullable: false),
                    MentalEmotionalRaw = table.Column<int>(type: "INTEGER", nullable: false),
                    MentalEmotionalMax = table.Column<int>(type: "INTEGER", nullable: false),
                    MentalEmotionalScore = table.Column<double>(type: "REAL", nullable: false),
                    ImmunityRaw = table.Column<int>(type: "INTEGER", nullable: false),
                    ImmunityMax = table.Column<int>(type: "INTEGER", nullable: false),
                    ImmunityScore = table.Column<double>(type: "REAL", nullable: false),
                    LongevityRaw = table.Column<int>(type: "INTEGER", nullable: false),
                    LongevityMax = table.Column<int>(type: "INTEGER", nullable: false),
                    LongevityScore = table.Column<double>(type: "REAL", nullable: false),
                    Bmi = table.Column<double>(type: "REAL", nullable: false),
                    WaistToHeightRatio = table.Column<double>(type: "REAL", nullable: false),
                    WaistToHipRatio = table.Column<double>(type: "REAL", nullable: false),
                    RestingHeartRate = table.Column<int>(type: "INTEGER", nullable: false),
                    HeartRateRecovery = table.Column<int>(type: "INTEGER", nullable: false),
                    BloodPressureSystolic = table.Column<int>(type: "INTEGER", nullable: false),
                    BloodPressureDiastolic = table.Column<int>(type: "INTEGER", nullable: false),
                    Sex = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AgeAtAssessment = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<double>(type: "REAL", nullable: false),
                    HeightUnit = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Weight = table.Column<double>(type: "REAL", nullable: false),
                    WeightUnit = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Waist = table.Column<double>(type: "REAL", nullable: false),
                    WaistUnit = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Hip = table.Column<double>(type: "REAL", nullable: false),
                    HipUnit = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    BodyFatPercentage = table.Column<double>(type: "REAL", nullable: false),
                    InputSnapshotJson = table.Column<string>(type: "TEXT", nullable: false)
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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParameterKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ParameterName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    RawValue = table.Column<double>(type: "REAL", nullable: true),
                    RawText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    NormalizedValue = table.Column<double>(type: "REAL", nullable: true),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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
