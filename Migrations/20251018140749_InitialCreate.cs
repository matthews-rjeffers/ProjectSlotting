using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

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
                    CRPDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UATDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CodeCompleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BufferPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 20m),
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
                name: "OnsiteSchedules",
                columns: table => new
                {
                    OnsiteScheduleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    WeekStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EngineerCount = table.Column<int>(type: "int", nullable: false),
                    TotalHours = table.Column<int>(type: "int", nullable: false, defaultValue: 40),
                    OnsiteType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "ProjectAllocations",
                columns: table => new
                {
                    AllocationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    SquadId = table.Column<int>(type: "int", nullable: false),
                    AllocationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AllocatedHours = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AllocationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "")
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
                        principalColumn: "SquadId");
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
                        principalColumn: "SquadId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OnsiteSchedules_ProjectId",
                table: "OnsiteSchedules",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAllocations_ProjectId_SquadId_AllocationDate_Type",
                table: "ProjectAllocations",
                columns: new[] { "ProjectId", "SquadId", "AllocationDate", "AllocationType" },
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
                filter: "([IsActive]=(1))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OnsiteSchedules");

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
