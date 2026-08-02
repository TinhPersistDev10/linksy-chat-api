using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using linksy_backend_api.Models;

#nullable disable

namespace linksy_backend_api.Infrastructure.Migrations
{
    [DbContext(typeof(LinksyDbContext))]
    [Migration("20260802120000_AddMessagePolls")]
    public partial class AddMessagePolls : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "message_polls",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("message_polls_pkey", x => x.message_id);
                    table.ForeignKey(
                        name: "message_polls_message_id_fkey",
                        column: x => x.message_id,
                        principalTable: "messages",
                        principalColumn: "message_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "message_polls_created_by_user_id_fkey",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "message_poll_options",
                columns: table => new
                {
                    option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("message_poll_options_pkey", x => x.option_id);
                    table.ForeignKey(
                        name: "message_poll_options_message_id_fkey",
                        column: x => x.message_id,
                        principalTable: "message_polls",
                        principalColumn: "message_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "message_poll_votes",
                columns: table => new
                {
                    vote_id = table.Column<Guid>(type: "uuid", nullable: false),
                    option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    voted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("message_poll_votes_pkey", x => x.vote_id);
                    table.ForeignKey(
                        name: "message_poll_votes_option_id_fkey",
                        column: x => x.option_id,
                        principalTable: "message_poll_options",
                        principalColumn: "option_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "message_poll_votes_message_id_fkey",
                        column: x => x.message_id,
                        principalTable: "message_polls",
                        principalColumn: "message_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "message_poll_votes_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "message_poll_options_message_id_idx",
                table: "message_poll_options",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "message_poll_options_message_id_sort_order_idx",
                table: "message_poll_options",
                columns: new[] { "message_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "message_poll_votes_message_id_user_id_key",
                table: "message_poll_votes",
                columns: new[] { "message_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "message_poll_votes_option_id_idx",
                table: "message_poll_votes",
                column: "option_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "message_poll_votes");
            migrationBuilder.DropTable(name: "message_poll_options");
            migrationBuilder.DropTable(name: "message_polls");
        }
    }
}
