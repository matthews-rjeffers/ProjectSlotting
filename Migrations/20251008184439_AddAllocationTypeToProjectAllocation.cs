using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScheduler.Migrations
{
    /// <inheritdoc />
    public partial class AddAllocationTypeToProjectAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllocationType",
                table: "ProjectAllocations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllocationType",
                table: "ProjectAllocations");
        }
    }
}
