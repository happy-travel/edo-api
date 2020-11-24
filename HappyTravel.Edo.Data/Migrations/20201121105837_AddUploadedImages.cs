using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace HappyTravel.Edo.Data.Migrations
{
    public partial class AddUploadedImages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UploadedImages",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AgencyId = table.Column<int>(nullable: false),
                    FileName = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: false),
                    Updated = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedImages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UploadedImages_AgencyId_FileName",
                table: "UploadedImages",
                columns: new[] { "AgencyId", "FileName" });

            migrationBuilder.Sql("UPDATE \"AgentAgencyRelations\" SET \"InAgencyPermissions\" = 32766 WHERE \"InAgencyPermissions\" in (16382, 16383);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UploadedImages");

            migrationBuilder.Sql("UPDATE \"AgentAgencyRelations\" SET \"InAgencyPermissions\" = 16382 WHERE \"InAgencyPermissions\" = 32766;");
        }
    }
}
