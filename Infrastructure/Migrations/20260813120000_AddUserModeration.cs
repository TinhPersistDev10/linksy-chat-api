using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using linksy_backend_api.Models;

#nullable disable

namespace linksy_backend_api.Infrastructure.Migrations
{
    [DbContext(typeof(LinksyDbContext))]
    [Migration("20260813120000_AddUserModeration")]
    public partial class AddUserModeration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "moderation_level",
                table: "users",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "none");

            migrationBuilder.AddColumn<string>(
                name: "moderation_reason",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "moderation_expires_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "moderated_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "moderated_by_admin_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "violation_points",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_flagged_for_review",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "users_moderation_level_idx",
                table: "users",
                column: "moderation_level");

            migrationBuilder.CreateIndex(
                name: "users_is_flagged_for_review_idx",
                table: "users",
                column: "is_flagged_for_review");

            migrationBuilder.AddForeignKey(
                name: "users_moderated_by_admin_id_fkey",
                table: "users",
                column: "moderated_by_admin_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "users_moderated_by_admin_id_fkey",
                table: "users");

            migrationBuilder.DropIndex(
                name: "users_moderation_level_idx",
                table: "users");

            migrationBuilder.DropIndex(
                name: "users_is_flagged_for_review_idx",
                table: "users");

            migrationBuilder.DropColumn(name: "moderation_level", table: "users");
            migrationBuilder.DropColumn(name: "moderation_reason", table: "users");
            migrationBuilder.DropColumn(name: "moderation_expires_at", table: "users");
            migrationBuilder.DropColumn(name: "moderated_at", table: "users");
            migrationBuilder.DropColumn(name: "moderated_by_admin_id", table: "users");
            migrationBuilder.DropColumn(name: "violation_points", table: "users");
            migrationBuilder.DropColumn(name: "is_flagged_for_review", table: "users");
        }
    }
}
