using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Edo.Data.Migrations
{
    public partial class ModifyTableNotifications2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationOptions_AgencyId_AgentId_Type",
                table: "NotificationOptions");

            migrationBuilder.RenameColumn(
                name: "AgentId",
                table: "NotificationOptions",
                newName: "UserType");

            migrationBuilder.DropColumn(
                name: "Message",
                table: "Notifications");

            migrationBuilder.AddColumn<JsonDocument>(
                name: "Message",
                table: "Notifications",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AgencyId",
                table: "Notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AgencyId",
                table: "NotificationOptions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "NotificationOptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOptions_AgencyId_UserId_UserType_Type",
                table: "NotificationOptions",
                columns: new[] { "AgencyId", "UserId", "UserType", "Type" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationOptions_AgencyId_UserId_UserType_Type",
                table: "NotificationOptions");

            migrationBuilder.DropColumn(
                name: "AgencyId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "NotificationOptions");

            migrationBuilder.RenameColumn(
                name: "UserType",
                table: "NotificationOptions",
                newName: "AgentId");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "Notifications",
                type: "text",
                nullable: true,
                oldClrType: typeof(JsonDocument),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AgencyId",
                table: "NotificationOptions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOptions_AgencyId_AgentId_Type",
                table: "NotificationOptions",
                columns: new[] { "AgencyId", "AgentId", "Type" },
                unique: true);
        }
    }
}
