using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using linksy_backend_api.Models;

#nullable disable

namespace linksy_backend_api.Infrastructure.Migrations
{
    [DbContext(typeof(LinksyDbContext))]
    [Migration("20260813100000_AddUserReports")]
    public partial class AddUserReports : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_reports",
                columns: table => new
                {
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporter_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reported_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    admin_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    reviewed_by_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_reports_pkey", x => x.report_id);
                    table.CheckConstraint("user_reports_reporter_ne_reported", "reporter_user_id <> reported_user_id");
                    table.ForeignKey(
                        name: "user_reports_reporter_user_id_fkey",
                        column: x => x.reporter_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "user_reports_reported_user_id_fkey",
                        column: x => x.reported_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "user_reports_reviewed_by_admin_id_fkey",
                        column: x => x.reviewed_by_admin_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "CHECK (reporter_user_id != reported_user_id)");

            migrationBuilder.CreateIndex(
                name: "user_reports_reported_user_id_status_idx",
                table: "user_reports",
                columns: new[] { "reported_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "user_reports_reporter_user_id_created_at_idx",
                table: "user_reports",
                columns: new[] { "reporter_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "user_reports_status_created_at_idx",
                table: "user_reports",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "user_reports_reporter_reported_pending_idx",
                table: "user_reports",
                columns: new[] { "reporter_user_id", "reported_user_id", "status" },
                unique: true,
                filter: "status = 'pending'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "user_reports");
        }
    }
}
