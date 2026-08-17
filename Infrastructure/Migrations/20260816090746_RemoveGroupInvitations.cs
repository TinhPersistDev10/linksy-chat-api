using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace linksy_backend_api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGroupInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "group_invitations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "group_invitations",
                columns: table => new
                {
                    invitation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chatroom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: false),
                    invited_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    message = table.Column<string>(type: "character varying", nullable: true),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    status = table.Column<string>(type: "character varying", nullable: false, defaultValueSql: "'pending'::character varying")
                },
                constraints: table =>
                {
                    table.PrimaryKey("group_invitations_pkey", x => x.invitation_id);
                    table.ForeignKey(
                        name: "group_invitations_chatroom_id_fkey",
                        column: x => x.chatroom_id,
                        principalTable: "chatrooms",
                        principalColumn: "chatroom_id");
                    table.ForeignKey(
                        name: "group_invitations_invited_by_fkey",
                        column: x => x.invited_by,
                        principalTable: "users",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "group_invitations_invited_user_id_fkey",
                        column: x => x.invited_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateIndex(
                name: "group_invitations_chatroom_id_invited_user_id_status_idx",
                table: "group_invitations",
                columns: new[] { "chatroom_id", "invited_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "group_invitations_chatroom_id_status_idx",
                table: "group_invitations",
                columns: new[] { "chatroom_id", "status" });

            migrationBuilder.CreateIndex(
                name: "group_invitations_expires_at_idx",
                table: "group_invitations",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "group_invitations_invited_user_id_status_sent_at_idx",
                table: "group_invitations",
                columns: new[] { "invited_user_id", "status", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "IX_group_invitations_invited_by",
                table: "group_invitations",
                column: "invited_by");
        }
    }
}
