using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace HappyTravel.Edo.Data.Migrations
{
    public partial class AddCreditCardPaymentsConfirmationAuditLogsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditCardPaymentConfirmationAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Created = table.Column<DateTime>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    UserType = table.Column<int>(nullable: false),
                    ReferenceCode = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardPaymentConfirmationAuditLogs", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditCardPaymentConfirmationAuditLogs");
        }
    }
}
