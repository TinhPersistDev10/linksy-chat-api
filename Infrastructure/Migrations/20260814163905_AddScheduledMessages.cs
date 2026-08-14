using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace linksy_backend_api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scheduled_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chatroom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    message_text = table.Column<string>(type: "text", nullable: false),
                    parent_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    send_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("scheduled_messages_pkey", x => x.id);
                    table.ForeignKey(
                        name: "scheduled_messages_chatroom_id_fkey",
                        column: x => x.chatroom_id,
                        principalTable: "chatrooms",
                        principalColumn: "chatroom_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "scheduled_messages_parent_message_id_fkey",
                        column: x => x.parent_message_id,
                        principalTable: "messages",
                        principalColumn: "message_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "scheduled_messages_sender_id_fkey",
                        column: x => x.sender_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_messages_parent_message_id",
                table: "scheduled_messages",
                column: "parent_message_id");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_messages_sender_id",
                table: "scheduled_messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "scheduled_messages_due_idx",
                table: "scheduled_messages",
                columns: new[] { "status", "send_at" });

            migrationBuilder.CreateIndex(
                name: "scheduled_messages_room_sender_status_idx",
                table: "scheduled_messages",
                columns: new[] { "chatroom_id", "sender_id", "status", "send_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scheduled_messages");
        }
    }
}
