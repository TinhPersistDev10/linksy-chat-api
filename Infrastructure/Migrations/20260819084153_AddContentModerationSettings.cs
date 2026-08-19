using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace linksy_backend_api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentModerationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_moderation_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    banned_words = table.Column<List<string>>(type: "text[]", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("content_moderation_settings_pkey", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "content_moderation_settings",
                columns: new[] { "id", "enabled", "banned_words" },
                values: new object[]
                {
                    1,
                    true,
                    new[]
                    {
                        "dit", "deo", "cac", "buoi", "dmm", "vcl", "clgt", "oc cho", "occho",
                        "fuck", "fucker", "motherfucker", "shit", "bitch", "asshole", "dick",
                        "pussy", "cunt", "ditmemay", "ditme", "giet", "kill", "chem"
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_moderation_settings");
        }
    }
}
