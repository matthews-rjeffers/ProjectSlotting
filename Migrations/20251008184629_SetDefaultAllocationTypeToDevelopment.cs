using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectScheduler.Migrations
{
    /// <inheritdoc />
    public partial class SetDefaultAllocationTypeToDevelopment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing records to have 'Development' as the default type
            migrationBuilder.Sql(
                "UPDATE ProjectAllocations SET AllocationType = 'Development' WHERE AllocationType IS NULL OR AllocationType = ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
