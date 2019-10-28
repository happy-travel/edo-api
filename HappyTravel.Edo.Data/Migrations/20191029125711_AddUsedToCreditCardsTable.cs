using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Edo.Data.Migrations
{
    public partial class AddUsedToCreditCardsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreditCardId",
                table: "ExternalPayments",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Used",
                table: "CreditCards",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreditCardId",
                table: "ExternalPayments");

            migrationBuilder.DropColumn(
                name: "Used",
                table: "CreditCards");
        }
    }
}
