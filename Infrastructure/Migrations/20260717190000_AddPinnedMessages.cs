using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using linksy_backend_api.Models;

#nullable disable

namespace linksy_backend_api.Infrastructure.Migrations
{
    [DbContext(typeof(LinksyDbContext))]
    [Migration("20260717190000_AddPinnedMessages")]
    public partial class AddPinnedMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pinned_messages",
                columns: table => new
                {
                    pinned_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chatroom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pinned_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pinned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pinned_messages_pkey", x => x.pinned_message_id);
                    table.ForeignKey(
                        name: "pinned_messages_chatroom_id_fkey",
                        column: x => x.chatroom_id,
                        principalTable: "chatrooms",
                        principalColumn: "chatroom_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "pinned_messages_message_id_fkey",
                        column: x => x.message_id,
                        principalTable: "messages",
                        principalColumn: "message_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "pinned_messages_pinned_by_user_id_fkey",
                        column: x => x.pinned_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "pinned_messages_chatroom_id_message_id_key",
                table: "pinned_messages",
                columns: new[] { "chatroom_id", "message_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "pinned_messages_chatroom_id_pinned_at_idx",
                table: "pinned_messages",
                columns: new[] { "chatroom_id", "pinned_at" });

            migrationBuilder.CreateIndex(
                name: "pinned_messages_message_id_idx",
                table: "pinned_messages",
                column: "message_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "pinned_messages");
        }
    }
}
