using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScheduler.Migrations.ProjectSchedulerDb
{
    /// <inheritdoc />
    public partial class AddCodeCompleteDateToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CodeCompleteDate",
                table: "Projects",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodeCompleteDate",
                table: "Projects");
        }
    }
}
