using HappyTravel.Edo.Data.Agents;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Edo.Data.Migrations
{
    public partial class RefactorUserInvoicesDataToStrongTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE public.""UserInvitations"" ALTER ""Data"" TYPE jsonb USING ""Data""::jsonb");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE public.""UserInvitations"" ALTER ""Data"" TYPE text");
        }
    }
}
