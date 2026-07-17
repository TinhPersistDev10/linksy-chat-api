using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using linksy_backend_api.Models;

#nullable disable

namespace linksy_backend_api.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(LinksyDbContext))]
    [Migration("20260717180000_AddMessageMentions")]
    public partial class AddMessageMentions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "message_mentions",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mentioned_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("message_mentions_pkey", x => new { x.message_id, x.mentioned_user_id });
                    table.ForeignKey(
                        name: "message_mentions_message_id_fkey",
                        column: x => x.message_id,
                        principalTable: "messages",
                        principalColumn: "message_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "message_mentions_mentioned_user_id_fkey",
                        column: x => x.mentioned_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "message_mentions_message_id_idx",
                table: "message_mentions",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "message_mentions_mentioned_user_id_idx",
                table: "message_mentions",
                column: "mentioned_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "message_mentions");
        }
    }
}
