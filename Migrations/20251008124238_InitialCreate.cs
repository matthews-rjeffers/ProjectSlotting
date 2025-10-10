using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProjectScheduler.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerState = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    EstimatedDevHours = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    EstimatedOnsiteHours = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    GoLiveDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CRPDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UATDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JiraLink = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ProjectId);
                });

            migrationBuilder.CreateTable(
                name: "Squads",
                columns: table => new
                {
                    SquadId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SquadName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SquadLeadName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Squads", x => x.SquadId);
                });

            migrationBuilder.CreateTable(
                name: "ProjectAllocations",
                columns: table => new
                {
                    AllocationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    SquadId = table.Column<int>(type: "int", nullable: false),
                    AllocationDate = table.Column<DateTime>(type: "date", nullable: false),
                    AllocatedHours = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectAllocations", x => x.AllocationId);
                    table.ForeignKey(
                        name: "FK_ProjectAllocations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectAllocations_Squads_SquadId",
                        column: x => x.SquadId,
                        principalTable: "Squads",
                        principalColumn: "SquadId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    TeamMemberId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SquadId = table.Column<int>(type: "int", nullable: false),
                    MemberName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DailyCapacityHours = table.Column<decimal>(type: "decimal(4,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.TeamMemberId);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Squads_SquadId",
                        column: x => x.SquadId,
                        principalTable: "Squads",
                        principalColumn: "SquadId",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "IX_ProjectAllocations_ProjectId_SquadId_AllocationDate",
                table: "ProjectAllocations",
                columns: new[] { "ProjectId", "SquadId", "AllocationDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAllocations_SquadId_AllocationDate",
                table: "ProjectAllocations",
                columns: new[] { "SquadId", "AllocationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CRPDate",
                table: "Projects",
                column: "CRPDate");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectNumber",
                table: "Projects",
                column: "ProjectNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Squads_SquadName",
                table: "Squads",
                column: "SquadName");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_SquadId",
                table: "TeamMembers",
                column: "SquadId",
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectAllocations");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Squads");
        }
    }
}
