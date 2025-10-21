using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScheduler.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceWeekStartDateWithStartEndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns with default values
            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "OnsiteSchedules",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 1, 1));

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "OnsiteSchedules",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 1, 7));

            // Copy data from old column to new columns
            // WeekStartDate → StartDate, WeekStartDate + 4 days → EndDate
            migrationBuilder.Sql(@"
                UPDATE OnsiteSchedules
                SET StartDate = WeekStartDate,
                    EndDate = DATEADD(day, 4, WeekStartDate)
            ");

            // Drop old column
            migrationBuilder.DropColumn(
                name: "WeekStartDate",
                table: "OnsiteSchedules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add back the old column
            migrationBuilder.AddColumn<DateTime>(
                name: "WeekStartDate",
                table: "OnsiteSchedules",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 1, 1));

            // Copy data back from StartDate to WeekStartDate
            migrationBuilder.Sql(@"
                UPDATE OnsiteSchedules
                SET WeekStartDate = StartDate
            ");

            // Drop new columns
            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "OnsiteSchedules");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "OnsiteSchedules");
        }
    }
}
