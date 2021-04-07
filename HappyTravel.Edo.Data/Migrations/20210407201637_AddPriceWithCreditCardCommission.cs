using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Edo.Data.Migrations
{
    public partial class AddPriceWithCreditCardCommission : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CreditCardPaymentPrice",
                table: "Bookings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
            
            migrationBuilder.Sql("UPDATE \"Bookings\" SET \"CreditCardPaymentPrice\" = \"TotalPrice\"");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreditCardPaymentPrice",
                table: "Bookings");
        }
    }
}
