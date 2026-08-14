using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace linksy_backend_api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFriendInvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "friend_invites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    inviter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("friend_invites_pkey", x => x.id);
                    table.ForeignKey(
                        name: "friend_invites_inviter_id_fkey",
                        column: x => x.inviter_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "friend_invites_inviter_active_idx",
                table: "friend_invites",
                columns: new[] { "inviter_id", "is_used", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "friend_invites_token_uidx",
                table: "friend_invites",
                column: "token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "friend_invites");
        }
    }
}
