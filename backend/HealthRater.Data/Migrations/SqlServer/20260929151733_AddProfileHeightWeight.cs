using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthRater.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddProfileHeightWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "HeightCm",
                table: "Users",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WeightKg",
                table: "Users",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "Users");
        }
    }
}
