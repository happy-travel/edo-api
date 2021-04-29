using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Edo.Data.Migrations
{
    public partial class RemovedDisplayedPaymentOptions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayedPaymentOptions",
                table: "AgencySystemSettings");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayedPaymentOptions",
                table: "AgencySystemSettings",
                type: "integer",
                nullable: true);
        }
    }
}
