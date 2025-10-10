using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProjectScheduler.Migrations
{
    /// <inheritdoc />
    public partial class AddOnsiteScheduleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectAllocations_Squads_SquadId",
                table: "ProjectAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamMembers_Squads_SquadId",
                table: "TeamMembers");

            migrationBuilder.DropIndex(
                name: "IX_TeamMembers_SquadId",
                table: "TeamMembers");

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "TeamMembers",
                keyColumn: "TeamMemberId",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Squads",
                keyColumn: "SquadId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Squads",
                keyColumn: "SquadId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Squads",
                keyColumn: "SquadId",
                keyValue: 3);

            migrationBuilder.AlterColumn<string>(
                name: "AllocationType",
                table: "ProjectAllocations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateTable(
                name: "OnsiteSchedules",
                columns: table => new
                {
                    OnsiteScheduleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    WeekStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EngineerCount = table.Column<int>(type: "int", nullable: false),
                    OnsiteType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnsiteSchedules", x => x.OnsiteScheduleId);
                    table.ForeignKey(
                        name: "FK_OnsiteSchedules_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_SquadId",
                table: "TeamMembers",
                column: "SquadId",
                filter: "([IsActive]=(1))");

            migrationBuilder.CreateIndex(
                name: "IX_OnsiteSchedules_ProjectId",
                table: "OnsiteSchedules",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectAllocations_Squads_SquadId",
                table: "ProjectAllocations",
                column: "SquadId",
                principalTable: "Squads",
                principalColumn: "SquadId");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamMembers_Squads_SquadId",
                table: "TeamMembers",
                column: "SquadId",
                principalTable: "Squads",
                principalColumn: "SquadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectAllocations_Squads_SquadId",
                table: "ProjectAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamMembers_Squads_SquadId",
                table: "TeamMembers");

            migrationBuilder.DropTable(
                name: "OnsiteSchedules");

            migrationBuilder.DropIndex(
                name: "IX_TeamMembers_SquadId",
                table: "TeamMembers");

            migrationBuilder.AlterColumn<string>(
                name: "AllocationType",
                table: "ProjectAllocations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "");

            migrationBuilder.InsertData(
                table: "Squads",
                columns: new[] { "SquadId", "IsActive", "SquadLeadName", "SquadName" },
                values: new object[,]
                {
                    { 1, true, "TBD", "Squad Alpha" },
                    { 2, true, "TBD", "Squad Beta" },
                    { 3, true, "TBD", "Squad Gamma" }
                });

            migrationBuilder.InsertData(
                table: "TeamMembers",
                columns: new[] { "TeamMemberId", "DailyCapacityHours", "IsActive", "MemberName", "Role", "SquadId" },
                values: new object[,]
                {
                    { 1, 6.5m, true, "Project Lead 1", "ProjectLead", 1 },
                    { 2, 6.5m, true, "Project Lead 2", "ProjectLead", 1 },
                    { 3, 6.5m, true, "Project Lead 3", "ProjectLead", 1 },
                    { 4, 6.5m, true, "Developer 1", "Developer", 1 },
                    { 5, 6.5m, true, "Developer 2", "Developer", 1 },
                    { 6, 6.5m, true, "Developer 3", "Developer", 1 },
                    { 7, 6.5m, true, "Developer 4", "Developer", 1 },
                    { 8, 6.5m, true, "Developer 5", "Developer", 1 },
                    { 9, 6.5m, true, "Developer 6", "Developer", 1 },
                    { 10, 6.5m, true, "Developer 7", "Developer", 1 },
                    { 11, 6.5m, true, "Developer 8", "Developer", 1 },
                    { 12, 6.5m, true, "Project Lead 1", "ProjectLead", 2 },
                    { 13, 6.5m, true, "Project Lead 2", "ProjectLead", 2 },
                    { 14, 6.5m, true, "Project Lead 3", "ProjectLead", 2 },
                    { 15, 6.5m, true, "Developer 1", "Developer", 2 },
                    { 16, 6.5m, true, "Developer 2", "Developer", 2 },
                    { 17, 6.5m, true, "Developer 3", "Developer", 2 },
                    { 18, 6.5m, true, "Developer 4", "Developer", 2 },
                    { 19, 6.5m, true, "Developer 5", "Developer", 2 },
                    { 20, 6.5m, true, "Developer 6", "Developer", 2 },
                    { 21, 6.5m, true, "Developer 7", "Developer", 2 },
                    { 22, 6.5m, true, "Developer 8", "Developer", 2 },
                    { 23, 6.5m, true, "Project Lead 1", "ProjectLead", 3 },
                    { 24, 6.5m, true, "Project Lead 2", "ProjectLead", 3 },
                    { 25, 6.5m, true, "Project Lead 3", "ProjectLead", 3 },
                    { 26, 6.5m, true, "Developer 1", "Developer", 3 },
                    { 27, 6.5m, true, "Developer 2", "Developer", 3 },
                    { 28, 6.5m, true, "Developer 3", "Developer", 3 },
                    { 29, 6.5m, true, "Developer 4", "Developer", 3 },
                    { 30, 6.5m, true, "Developer 5", "Developer", 3 },
                    { 31, 6.5m, true, "Developer 6", "Developer", 3 },
                    { 32, 6.5m, true, "Developer 7", "Developer", 3 },
                    { 33, 6.5m, true, "Developer 8", "Developer", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_SquadId",
                table: "TeamMembers",
                column: "SquadId",
                filter: "[IsActive] = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectAllocations_Squads_SquadId",
                table: "ProjectAllocations",
                column: "SquadId",
                principalTable: "Squads",
                principalColumn: "SquadId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamMembers_Squads_SquadId",
                table: "TeamMembers",
                column: "SquadId",
                principalTable: "Squads",
                principalColumn: "SquadId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
