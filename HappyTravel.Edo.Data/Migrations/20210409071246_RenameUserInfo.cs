using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Edo.Data.Migrations
{
    public partial class RenameUserInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserType",
                table: "OfflinePaymentAuditLogs",
                newName: "ApiCallerType");

            migrationBuilder.RenameColumn(
                name: "UserType",
                table: "MarkupPolicyAuditLogs",
                newName: "ApiCallerType");

            migrationBuilder.RenameColumn(
                name: "UserType",
                table: "CreditCardAuditLogs",
                newName: "ApiCallerType");

            migrationBuilder.RenameColumn(
                name: "UserType",
                table: "BookingStatusHistory",
                newName: "ApiCallerType");

            migrationBuilder.RenameIndex(
                name: "IX_BookingStatusHistory_UserType",
                table: "BookingStatusHistory",
                newName: "IX_BookingStatusHistory_ApiCallerType");

            migrationBuilder.RenameColumn(
                name: "UserType",
                table: "AccountBalanceAuditLogs",
                newName: "ApiCallerType");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ApiCallerType",
                table: "OfflinePaymentAuditLogs",
                newName: "UserType");

            migrationBuilder.RenameColumn(
                name: "ApiCallerType",
                table: "MarkupPolicyAuditLogs",
                newName: "UserType");

            migrationBuilder.RenameColumn(
                name: "ApiCallerType",
                table: "CreditCardAuditLogs",
                newName: "UserType");

            migrationBuilder.RenameColumn(
                name: "ApiCallerType",
                table: "BookingStatusHistory",
                newName: "UserType");

            migrationBuilder.RenameIndex(
                name: "IX_BookingStatusHistory_ApiCallerType",
                table: "BookingStatusHistory",
                newName: "IX_BookingStatusHistory_UserType");

            migrationBuilder.RenameColumn(
                name: "ApiCallerType",
                table: "AccountBalanceAuditLogs",
                newName: "UserType");
        }
    }
}
