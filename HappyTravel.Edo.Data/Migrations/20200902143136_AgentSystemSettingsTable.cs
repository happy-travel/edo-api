using HappyTravel.Edo.Data.Agents;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Edo.Data.Migrations
{
    public partial class AgentSystemSettingsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentSystemSettings",
                columns: table => new
                {
                    AgentId = table.Column<int>(nullable: false),
                    AgencyId = table.Column<int>(nullable: false),
                    AvailabilitySearchSettings = table.Column<AgentAvailabilitySearchSettings>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentSystemSettings", x => new { x.AgentId, x.AgencyId });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentSystemSettings");
        }
    }
}
