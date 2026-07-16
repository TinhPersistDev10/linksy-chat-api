using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace linksy_backend_api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSettingsStatusAndMessageFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ================================================================
            // SECTION 1: Safe column additions to chatroom_members
            // Using "IF NOT EXISTS" via raw SQL because the table was created
            // directly in DBeaver, so some columns may already exist.
            // ================================================================
            migrationBuilder.Sql(@"
                ALTER TABLE chatroom_members ADD COLUMN IF NOT EXISTS added_by uuid NULL;
                ALTER TABLE chatroom_members ADD COLUMN IF NOT EXISTS last_read_at timestamptz NULL;
                ALTER TABLE chatroom_members ADD COLUMN IF NOT EXISTS message_count integer NULL DEFAULT 0;
                ALTER TABLE chatroom_members ADD COLUMN IF NOT EXISTS muted_until timestamptz NULL;
                ALTER TABLE chatroom_members ADD COLUMN IF NOT EXISTS nickname character varying(100) NULL;
                ALTER TABLE chatroom_members ADD COLUMN IF NOT EXISTS notification_preference character varying(50) NULL DEFAULT 'all';
                ALTER TABLE chatroom_members ADD COLUMN IF NOT EXISTS removed_by uuid NULL;
            ");

            // Safe index creation — won't fail if index already exists
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS chatroom_members_chatroom_id_last_read_at_idx
                    ON chatroom_members (chatroom_id, last_read_at);
                CREATE INDEX IF NOT EXISTS IX_chatroom_members_added_by
                    ON chatroom_members (added_by);
                CREATE INDEX IF NOT EXISTS IX_chatroom_members_removed_by
                    ON chatroom_members (removed_by);
            ");

            // Safe FK additions — only add if they don't already exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'chatroom_members_added_by_fkey'
                    ) THEN
                        ALTER TABLE chatroom_members
                            ADD CONSTRAINT chatroom_members_added_by_fkey
                            FOREIGN KEY (added_by) REFERENCES users(user_id) ON DELETE SET NULL;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'chatroom_members_removed_by_fkey'
                    ) THEN
                        ALTER TABLE chatroom_members
                            ADD CONSTRAINT chatroom_members_removed_by_fkey
                            FOREIGN KEY (removed_by) REFERENCES users(user_id) ON DELETE SET NULL;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'chatroom_members_chatroom_id_fkey'
                    ) THEN
                        ALTER TABLE chatroom_members
                            ADD CONSTRAINT chatroom_members_chatroom_id_fkey
                            FOREIGN KEY (chatroom_id) REFERENCES chatrooms(chatroom_id) ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'chatroom_members_user_id_fkey'
                    ) THEN
                        ALTER TABLE chatroom_members
                            ADD CONSTRAINT chatroom_members_user_id_fkey
                            FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE;
                    END IF;
                END
                $$;
            ");

            // ================================================================
            // SECTION 2: Create member_permissions (IF NOT EXISTS)
            // ================================================================
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS member_permissions (
                    permission_id       uuid        NOT NULL,
                    member_id           uuid        NOT NULL,
                    can_send_messages   boolean     NOT NULL DEFAULT true,
                    can_send_media      boolean     NOT NULL DEFAULT true,
                    can_send_voice      boolean     NOT NULL DEFAULT true,
                    can_send_files      boolean     NOT NULL DEFAULT true,
                    can_invite_members  boolean     NOT NULL DEFAULT true,
                    can_remove_members  boolean     NOT NULL DEFAULT false,
                    can_edit_group_info boolean     NOT NULL DEFAULT false,
                    can_pin_messages    boolean     NOT NULL DEFAULT true,
                    can_delete_messages boolean     NOT NULL DEFAULT false,
                    can_manage_calls    boolean     NOT NULL DEFAULT true,
                    created_at          timestamptz NOT NULL DEFAULT now(),
                    updated_at          timestamptz NOT NULL DEFAULT now(),
                    CONSTRAINT permission_members_pkey PRIMARY KEY (permission_id),
                    CONSTRAINT permission_members_member_id_fkey
                        FOREIGN KEY (member_id) REFERENCES chatroom_members(member_id) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS permission_members_member_id_idx
                    ON member_permissions (member_id);
            ");

            // ================================================================
            // SECTION 3: Create 7 new tables (all IF NOT EXISTS)
            // ================================================================

            migrationBuilder.CreateTable(
                name: "message_attachments",
                columns: table => new
                {
                    attachment_id   = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id      = table.Column<Guid>(type: "uuid", nullable: false),
                    attachment_type = table.Column<string>(type: "character varying", nullable: false),
                    file_name       = table.Column<string>(type: "character varying", nullable: true),
                    file_path       = table.Column<string>(type: "text", nullable: true),
                    file_size       = table.Column<long>(type: "bigint", nullable: true),
                    mime_type       = table.Column<string>(type: "character varying", nullable: true),
                    cdn_url         = table.Column<string>(type: "text", nullable: true),
                    thumbnail_url   = table.Column<string>(type: "text", nullable: true),
                    width           = table.Column<int>(type: "integer", nullable: true),
                    height          = table.Column<int>(type: "integer", nullable: true),
                    duration_ms     = table.Column<int>(type: "integer", nullable: true),
                    uploaded_at     = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("message_attachments_pkey", x => x.attachment_id);
                    table.ForeignKey(
                        name: "message_attachments_message_id_fkey",
                        column: x => x.message_id,
                        principalTable: "messages",
                        principalColumn: "message_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "message_deliveries",
                columns: table => new
                {
                    delivery_id  = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id   = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id      = table.Column<Guid>(type: "uuid", nullable: false),
                    status       = table.Column<string>(type: "character varying", nullable: false, defaultValueSql: "'sent'::character varying"),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    read_at      = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("message_deliveries_pkey", x => x.delivery_id);
                    table.ForeignKey(
                        name: "message_deliveries_message_id_fkey",
                        column: x => x.message_id,
                        principalTable: "messages",
                        principalColumn: "message_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "message_deliveries_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "message_reactions",
                columns: table => new
                {
                    reaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id  = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id     = table.Column<Guid>(type: "uuid", nullable: false),
                    emoji_code  = table.Column<string>(type: "character varying", nullable: false),
                    reacted_at  = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("message_reactions_pkey", x => x.reaction_id);
                    table.ForeignKey(
                        name: "message_reactions_message_id_fkey",
                        column: x => x.message_id,
                        principalTable: "messages",
                        principalColumn: "message_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "message_reactions_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_settings",
                columns: table => new
                {
                    id                         = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id                    = table.Column<Guid>(type: "uuid", nullable: false),
                    notifications_enabled      = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notification_sound_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    message_preview_enabled    = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    email_notifications        = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("notification_settings_pkey", x => x.id);
                    table.ForeignKey(
                        name: "notification_settings_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "privacy_settings",
                columns: table => new
                {
                    id                        = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id                   = table.Column<Guid>(type: "uuid", nullable: false),
                    read_receipts_enabled     = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    typing_indicators_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_seen_enabled         = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    profile_photo_visibility  = table.Column<string>(type: "character varying", nullable: false, defaultValueSql: "'everyone'::character varying"),
                    status_visibility         = table.Column<string>(type: "character varying", nullable: false, defaultValueSql: "'everyone'::character varying"),
                    who_can_add_to_groups     = table.Column<string>(type: "character varying", nullable: false, defaultValueSql: "'everyone'::character varying")
                },
                constraints: table =>
                {
                    table.PrimaryKey("privacy_settings_pkey", x => x.id);
                    table.ForeignKey(
                        name: "privacy_settings_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_settings",
                columns: table => new
                {
                    setting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id    = table.Column<Guid>(type: "uuid", nullable: false),
                    language   = table.Column<string>(type: "character varying", nullable: false, defaultValueSql: "'vi'::character varying"),
                    timezone   = table.Column<string>(type: "character varying", nullable: false, defaultValueSql: "'Asia/Ho_Chi_Minh'::character varying"),
                    theme      = table.Column<string>(type: "character varying", nullable: false, defaultValueSql: "'system'::character varying"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_settings_pkey", x => x.setting_id);
                    table.ForeignKey(
                        name: "user_settings_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_status",
                columns: table => new
                {
                    status_id     = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id       = table.Column<Guid>(type: "uuid", nullable: false),
                    status_type   = table.Column<string>(type: "character varying", nullable: false, defaultValueSql: "'offline'::character varying"),
                    custom_status = table.Column<string>(type: "character varying", nullable: true),
                    last_seen_at  = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at    = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_status_pkey", x => x.status_id);
                    table.ForeignKey(
                        name: "user_status_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ================================================================
            // SECTION 4: Indexes for new tables
            // ================================================================
            migrationBuilder.CreateIndex(
                name: "message_attachments_message_id_attachment_type_idx",
                table: "message_attachments",
                columns: new[] { "message_id", "attachment_type" });

            migrationBuilder.CreateIndex(
                name: "message_attachments_message_id_idx",
                table: "message_attachments",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "message_deliveries_message_id_status_idx",
                table: "message_deliveries",
                columns: new[] { "message_id", "status" });

            migrationBuilder.CreateIndex(
                name: "message_deliveries_message_id_user_id_key",
                table: "message_deliveries",
                columns: new[] { "message_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "message_deliveries_user_id_status_read_at_idx",
                table: "message_deliveries",
                columns: new[] { "user_id", "status", "read_at" });

            migrationBuilder.CreateIndex(
                name: "message_reactions_message_id_emoji_code_idx",
                table: "message_reactions",
                columns: new[] { "message_id", "emoji_code" });

            migrationBuilder.CreateIndex(
                name: "message_reactions_message_id_user_id_emoji_code_key",
                table: "message_reactions",
                columns: new[] { "message_id", "user_id", "emoji_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "message_reactions_user_id_reacted_at_idx",
                table: "message_reactions",
                columns: new[] { "user_id", "reacted_at" });

            migrationBuilder.CreateIndex(
                name: "notification_settings_user_id_key",
                table: "notification_settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "privacy_settings_user_id_key",
                table: "privacy_settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "user_settings_user_id_key",
                table: "user_settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "user_status_user_id_key",
                table: "user_status",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "user_status_user_id_status_type_idx",
                table: "user_status",
                columns: new[] { "user_id", "status_type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop new tables in reverse FK order
            migrationBuilder.DropTable(name: "message_attachments");
            migrationBuilder.DropTable(name: "message_deliveries");
            migrationBuilder.DropTable(name: "message_reactions");
            migrationBuilder.DropTable(name: "notification_settings");
            migrationBuilder.DropTable(name: "privacy_settings");
            migrationBuilder.DropTable(name: "user_settings");
            migrationBuilder.DropTable(name: "user_status");

            // Drop member_permissions via raw SQL (was created with IF NOT EXISTS)
            migrationBuilder.Sql("DROP TABLE IF EXISTS member_permissions;");

            // Remove columns added to chatroom_members
            migrationBuilder.Sql(@"
                ALTER TABLE chatroom_members DROP COLUMN IF EXISTS added_by;
                ALTER TABLE chatroom_members DROP COLUMN IF EXISTS last_read_at;
                ALTER TABLE chatroom_members DROP COLUMN IF EXISTS message_count;
                ALTER TABLE chatroom_members DROP COLUMN IF EXISTS muted_until;
                ALTER TABLE chatroom_members DROP COLUMN IF EXISTS nickname;
                ALTER TABLE chatroom_members DROP COLUMN IF EXISTS notification_preference;
                ALTER TABLE chatroom_members DROP COLUMN IF EXISTS removed_by;
            ");
        }
    }
}