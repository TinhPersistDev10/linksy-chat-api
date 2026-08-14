using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using linksy_backend_api.Models;

#nullable disable

namespace linksy_backend_api.Infrastructure.Migrations
{
    [DbContext(typeof(LinksyDbContext))]
    [Migration("20260806160000_AddWhoCanMessageMe")]
    public partial class AddWhoCanMessageMe : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "who_can_message_me",
                table: "privacy_settings",
                type: "character varying",
                nullable: false,
                defaultValueSql: "'everyone'::character varying");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "who_can_message_me",
                table: "privacy_settings");
        }
    }
}
