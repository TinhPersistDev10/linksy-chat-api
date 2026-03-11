using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace linksy_backend_api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameToChatroomMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Rename table role_members to chatroom_members
            migrationBuilder.RenameTable(
                name: "role_members",
                newName: "chatroom_members");

            // 2. Rename primary key constraint
            migrationBuilder.RenameIndex(
                name: "role_members_pkey",
                table: "chatroom_members",
                newName: "chatroom_members_pkey");

            // 3. Rename indexes
            migrationBuilder.RenameIndex(
                name: "role_members_chatroom_id_member_role_idx",
                table: "chatroom_members",
                newName: "chatroom_members_chatroom_id_member_role_idx");

            migrationBuilder.RenameIndex(
                name: "role_members_chatroom_id_user_id_idx",
                table: "chatroom_members",
                newName: "chatroom_members_chatroom_id_user_id_idx");

            migrationBuilder.RenameIndex(
                name: "role_members_user_id_chatroom_id_idx",
                table: "chatroom_members",
                newName: "chatroom_members_user_id_chatroom_id_idx");

            migrationBuilder.RenameIndex(
                name: "role_members_user_id_left_at_idx",
                table: "chatroom_members",
                newName: "chatroom_members_user_id_left_at_idx");

            // 4. Drop old permission columns
            migrationBuilder.DropColumn(
                name: "can_send_message",
                table: "chatroom_members");

            migrationBuilder.DropColumn(
                name: "can_invite_members",
                table: "chatroom_members");

            migrationBuilder.DropColumn(
                name: "can_edit_group_info",
                table: "chatroom_members");

            migrationBuilder.DropColumn(
                name: "can_delete_messages",
                table: "chatroom_members");

            migrationBuilder.DropColumn(
                name: "can_remove_members",
                table: "chatroom_members");

            // 5. Add new columns
            migrationBuilder.AddColumn<string>(
                name: "nickname",
                table: "chatroom_members",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_muted",
                table: "chatroom_members",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "muted_until",
                table: "chatroom_members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notification_preference",
                table: "chatroom_members",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                defaultValueSql: "'all'::character varying");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_read_at",
                table: "chatroom_members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "message_count",
                table: "chatroom_members",
                type: "integer",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "added_by",
                table: "chatroom_members",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "removed_by",
                table: "chatroom_members",
                type: "uuid",
                nullable: true);

            // 6. Create member_permissions table
            migrationBuilder.CreateTable(
                name: "member_permissions",
                columns: table => new
                {
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    can_send_messages = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    can_send_media = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    can_send_voice = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    can_send_files = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    can_invite_members = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    can_remove_members = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    can_edit_group_info = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    can_pin_messages = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    can_delete_messages = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    can_manage_calls = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("member_permissions_pkey", x => x.permission_id);
                    table.ForeignKey(
                        name: "member_permissions_member_id_fkey",
                        column: x => x.member_id,
                        principalTable: "chatroom_members",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 7. Create index for member_permissions
            migrationBuilder.CreateIndex(
                name: "member_permissions_member_id_idx",
                table: "member_permissions",
                column: "member_id",
                unique: true);

            // 8. Create indexes for added_by and removed_by
            migrationBuilder.CreateIndex(
                name: "IX_chatroom_members_added_by",
                table: "chatroom_members",
                column: "added_by");

            migrationBuilder.CreateIndex(
                name: "IX_chatroom_members_removed_by",
                table: "chatroom_members",
                column: "removed_by");

            // 9. Add foreign keys for added_by and removed_by
            migrationBuilder.AddForeignKey(
                name: "chatroom_members_added_by_fkey",
                table: "chatroom_members",
                column: "added_by",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "chatroom_members_removed_by_fkey",
                table: "chatroom_members",
                column: "removed_by",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Drop foreign keys
            migrationBuilder.DropForeignKey(
                name: "chatroom_members_added_by_fkey",
                table: "chatroom_members");

            migrationBuilder.DropForeignKey(
                name: "chatroom_members_removed_by_fkey",
                table: "chatroom_members");

            // 2. Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_chatroom_members_added_by",
                table: "chatroom_members");

            migrationBuilder.DropIndex(
                name: "IX_chatroom_members_removed_by",
                table: "chatroom_members");

            // 3. Drop member_permissions table
            migrationBuilder.DropTable(
                name: "member_permissions");

            // 4. Drop new columns
            migrationBuilder.DropColumn(
                name: "nickname",
                table: "chatroom_members");

            migrationBuilder.DropColumn(
                name: "is_muted",
                table: "chatroom_members");

            migrationBuilder.DropColumn(
                name: "muted_until",
                table: "chatroom_members");

            migrationBuilder.DropColumn(
                name: "notification_preference",
                table: "chatroom_members");

            migrationBuilder.DropColumn(
                name: "last_read_at",
                table: "chatroom_members");

            migrationBuilder.DropColumn(
                name: "message_count",
                table: "chatroom_members");

            migrationBuilder.DropColumn(
                name: "added_by",
                table: "chatroom_members");

            migrationBuilder.DropColumn(
                name: "removed_by",
                table: "chatroom_members");

            // 5. Add back old permission columns
            migrationBuilder.AddColumn<bool>(
                name: "can_send_message",
                table: "chatroom_members",
                type: "boolean",
                nullable: true,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "can_invite_members",
                table: "chatroom_members",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_edit_group_info",
                table: "chatroom_members",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_delete_messages",
                table: "chatroom_members",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_remove_members",
                table: "chatroom_members",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            // 6. Rename indexes back
            migrationBuilder.RenameIndex(
                name: "chatroom_members_user_id_left_at_idx",
                table: "chatroom_members",
                newName: "role_members_user_id_left_at_idx");

            migrationBuilder.RenameIndex(
                name: "chatroom_members_user_id_chatroom_id_idx",
                table: "chatroom_members",
                newName: "role_members_user_id_chatroom_id_idx");

            migrationBuilder.RenameIndex(
                name: "chatroom_members_chatroom_id_user_id_idx",
                table: "chatroom_members",
                newName: "role_members_chatroom_id_user_id_idx");

            migrationBuilder.RenameIndex(
                name: "chatroom_members_chatroom_id_member_role_idx",
                table: "chatroom_members",
                newName: "role_members_chatroom_id_member_role_idx");

            // 7. Rename primary key back
            migrationBuilder.RenameIndex(
                name: "chatroom_members_pkey",
                table: "chatroom_members",
                newName: "role_members_pkey");

            // 8. Rename table back
            migrationBuilder.RenameTable(
                name: "chatroom_members",
                newName: "role_members");
        }
    }
}
