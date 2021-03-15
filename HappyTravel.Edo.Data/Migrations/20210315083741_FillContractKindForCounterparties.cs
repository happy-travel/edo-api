using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Edo.Data.Migrations
{
    public partial class FillContractKindForCounterparties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE  ""Counterparties"" 
                            SET ""ContractKind"" = CASE WHEN ""PreferredPaymentMethod"" = 1 THEN 1 WHEN ""PreferredPaymentMethod"" = 2 THEN 3 END
                            WHERE ""State"" = 1 AND ""ContractKind"" IS NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
