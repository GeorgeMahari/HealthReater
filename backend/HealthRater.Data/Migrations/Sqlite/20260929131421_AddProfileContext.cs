using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthRater.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddProfileContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Sex",
                table: "HealthAssessments",
                newName: "SexAtAssessment");

            migrationBuilder.AddColumn<string>(
                name: "Sex",
                table: "Users",
                type: "TEXT",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirthAtAssessment",
                table: "HealthAssessments",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sex",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DateOfBirthAtAssessment",
                table: "HealthAssessments");

            migrationBuilder.RenameColumn(
                name: "SexAtAssessment",
                table: "HealthAssessments",
                newName: "Sex");
        }
    }
}
