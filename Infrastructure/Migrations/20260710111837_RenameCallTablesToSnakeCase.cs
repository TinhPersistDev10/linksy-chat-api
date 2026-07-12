using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace linksy_backend_api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameCallTablesToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CallLogs_chatrooms_ChatroomId",
                table: "CallLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_CallLogs_users_CallerId",
                table: "CallLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_CallParticipants_CallLogs_CallLogId",
                table: "CallParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_CallParticipants_users_UserId",
                table: "CallParticipants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CallParticipants",
                table: "CallParticipants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CallLogs",
                table: "CallLogs");

            migrationBuilder.RenameTable(
                name: "CallParticipants",
                newName: "call_participants");

            migrationBuilder.RenameTable(
                name: "CallLogs",
                newName: "call_logs");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "call_participants",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "call_participants",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "call_participants",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "LeftAt",
                table: "call_participants",
                newName: "left_at");

            migrationBuilder.RenameColumn(
                name: "JoinedAt",
                table: "call_participants",
                newName: "joined_at");

            migrationBuilder.RenameColumn(
                name: "CallLogId",
                table: "call_participants",
                newName: "call_log_id");

            migrationBuilder.RenameIndex(
                name: "IX_CallParticipants_UserId",
                table: "call_participants",
                newName: "IX_call_participants_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_CallParticipants_CallLogId_UserId",
                table: "call_participants",
                newName: "ix_call_participants_call_log_id_user_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "call_logs",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "call_logs",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "StartedAt",
                table: "call_logs",
                newName: "started_at");

            migrationBuilder.RenameColumn(
                name: "EndedAt",
                table: "call_logs",
                newName: "ended_at");

            migrationBuilder.RenameColumn(
                name: "DurationSec",
                table: "call_logs",
                newName: "duration_sec");

            migrationBuilder.RenameColumn(
                name: "ChatroomId",
                table: "call_logs",
                newName: "chatroom_id");

            migrationBuilder.RenameColumn(
                name: "CallerId",
                table: "call_logs",
                newName: "caller_id");

            migrationBuilder.RenameColumn(
                name: "CallType",
                table: "call_logs",
                newName: "call_type");

            migrationBuilder.RenameColumn(
                name: "AnsweredAt",
                table: "call_logs",
                newName: "answered_at");

            migrationBuilder.RenameIndex(
                name: "IX_CallLogs_ChatroomId_StartedAt",
                table: "call_logs",
                newName: "ix_call_logs_chatroom_id_started_at");

            migrationBuilder.RenameIndex(
                name: "IX_CallLogs_CallerId_StartedAt",
                table: "call_logs",
                newName: "ix_call_logs_caller_id_started_at");

            migrationBuilder.AddPrimaryKey(
                name: "PK_call_participants",
                table: "call_participants",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_call_logs",
                table: "call_logs",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_call_logs_chatrooms_chatroom_id",
                table: "call_logs",
                column: "chatroom_id",
                principalTable: "chatrooms",
                principalColumn: "chatroom_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_call_logs_users_caller_id",
                table: "call_logs",
                column: "caller_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_call_participants_call_logs_call_log_id",
                table: "call_participants",
                column: "call_log_id",
                principalTable: "call_logs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_call_participants_users_user_id",
                table: "call_participants",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_call_logs_chatrooms_chatroom_id",
                table: "call_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_call_logs_users_caller_id",
                table: "call_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_call_participants_call_logs_call_log_id",
                table: "call_participants");

            migrationBuilder.DropForeignKey(
                name: "FK_call_participants_users_user_id",
                table: "call_participants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_call_participants",
                table: "call_participants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_call_logs",
                table: "call_logs");

            migrationBuilder.RenameTable(
                name: "call_participants",
                newName: "CallParticipants");

            migrationBuilder.RenameTable(
                name: "call_logs",
                newName: "CallLogs");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "CallParticipants",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "CallParticipants",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "CallParticipants",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "left_at",
                table: "CallParticipants",
                newName: "LeftAt");

            migrationBuilder.RenameColumn(
                name: "joined_at",
                table: "CallParticipants",
                newName: "JoinedAt");

            migrationBuilder.RenameColumn(
                name: "call_log_id",
                table: "CallParticipants",
                newName: "CallLogId");

            migrationBuilder.RenameIndex(
                name: "IX_call_participants_user_id",
                table: "CallParticipants",
                newName: "IX_CallParticipants_UserId");

            migrationBuilder.RenameIndex(
                name: "ix_call_participants_call_log_id_user_id",
                table: "CallParticipants",
                newName: "IX_CallParticipants_CallLogId_UserId");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "CallLogs",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "CallLogs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "started_at",
                table: "CallLogs",
                newName: "StartedAt");

            migrationBuilder.RenameColumn(
                name: "ended_at",
                table: "CallLogs",
                newName: "EndedAt");

            migrationBuilder.RenameColumn(
                name: "duration_sec",
                table: "CallLogs",
                newName: "DurationSec");

            migrationBuilder.RenameColumn(
                name: "chatroom_id",
                table: "CallLogs",
                newName: "ChatroomId");

            migrationBuilder.RenameColumn(
                name: "caller_id",
                table: "CallLogs",
                newName: "CallerId");

            migrationBuilder.RenameColumn(
                name: "call_type",
                table: "CallLogs",
                newName: "CallType");

            migrationBuilder.RenameColumn(
                name: "answered_at",
                table: "CallLogs",
                newName: "AnsweredAt");

            migrationBuilder.RenameIndex(
                name: "ix_call_logs_chatroom_id_started_at",
                table: "CallLogs",
                newName: "IX_CallLogs_ChatroomId_StartedAt");

            migrationBuilder.RenameIndex(
                name: "ix_call_logs_caller_id_started_at",
                table: "CallLogs",
                newName: "IX_CallLogs_CallerId_StartedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CallParticipants",
                table: "CallParticipants",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CallLogs",
                table: "CallLogs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CallLogs_chatrooms_ChatroomId",
                table: "CallLogs",
                column: "ChatroomId",
                principalTable: "chatrooms",
                principalColumn: "chatroom_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CallLogs_users_CallerId",
                table: "CallLogs",
                column: "CallerId",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CallParticipants_CallLogs_CallLogId",
                table: "CallParticipants",
                column: "CallLogId",
                principalTable: "CallLogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CallParticipants_users_UserId",
                table: "CallParticipants",
                column: "UserId",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
