using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Edo.Data.Migrations
{
    public partial class MoveMarkupFormulaToRelation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayedMarkupFormula",
                table: "AgentAgencyRelations",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"AgentAgencyRelations\" r " +
                "SET \"DisplayedMarkupFormula\" = (SELECT a.\"DisplayedMarkupFormula\" FROM \"Agents\" a WHERE a.\"Id\" = r.\"AgentId\")");

            migrationBuilder.DropColumn(
                name: "DisplayedMarkupFormula",
                table: "Agents");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayedMarkupFormula",
                table: "Agents",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"Agents\" a " +
                "SET \"DisplayedMarkupFormula\" = (SELECT r.\"DisplayedMarkupFormula\" FROM \"AgentAgencyRelations\" r WHERE a.\"Id\" = r.\"AgentId\")");

            migrationBuilder.DropColumn(
                name: "DisplayedMarkupFormula",
                table: "AgentAgencyRelations");
        }
    }
}
