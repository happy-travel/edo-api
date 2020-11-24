using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Edo.Data.Migrations
{
    public partial class ChangeAllPrivilegesValue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"AgentAgencyRelations\" SET \"InAgencyPermissions\" = 2147483646 WHERE \"InAgencyPermissions\" in (16382, 16383, 32766);"
                );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
