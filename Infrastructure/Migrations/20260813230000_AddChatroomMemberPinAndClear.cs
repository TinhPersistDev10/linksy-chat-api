using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using linksy_backend_api.Models;

#nullable disable

namespace linksy_backend_api.Infrastructure.Migrations
{
    [DbContext(typeof(LinksyDbContext))]
    [Migration("20260813230000_AddChatroomMemberPinAndClear")]
    public partial class AddChatroomMemberPinAndClear : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_pinned",
                table: "chatroom_members",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "pinned_at",
                table: "chatroom_members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "cleared_at",
                table: "chatroom_members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "chatroom_members_user_id_is_pinned_pinned_at_idx",
                table: "chatroom_members",
                columns: new[] { "user_id", "is_pinned", "pinned_at" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "chatroom_members_user_id_is_pinned_pinned_at_idx",
                table: "chatroom_members");

            migrationBuilder.DropColumn(name: "is_pinned", table: "chatroom_members");
            migrationBuilder.DropColumn(name: "pinned_at", table: "chatroom_members");
            migrationBuilder.DropColumn(name: "cleared_at", table: "chatroom_members");
        }
    }
}
