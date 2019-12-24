using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Edo.Data.Migrations
{
    public partial class PartialPayments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferenceCode",
                table: "AccountBalanceAuditLogs",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Companies",
                keyColumn: "Id",
                keyValue: -1,
                column: "PreferredPaymentMethod",
                value: 2);

            migrationBuilder.Sql(@"update ""AccountBalanceAuditLogs""
            set ""ReferenceCode"" = ""EventData""::json->'referenceCode';");

            migrationBuilder.Sql(@"update ""ExternalPayments""
            set ""Data"" = ""Data"" || jsonb_build_object('internalReferenceCode', ""ReferenceCode"")
            from ""Bookings""
            where ""BookingId"" = ""Bookings"".""Id"";");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferenceCode",
                table: "AccountBalanceAuditLogs");

            migrationBuilder.UpdateData(
                table: "Companies",
                keyColumn: "Id",
                keyValue: -1,
                column: "PreferredPaymentMethod",
                value: 1);
        }
    }
}
