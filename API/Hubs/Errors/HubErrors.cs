using Microsoft.AspNetCore.SignalR;

namespace linksy_backend_api.API.Hubs.Errors;

/// <summary>
/// Factory tạo HubException với format "CODE|message".
/// Client split theo '|' để lấy code và message riêng biệt.
/// </summary>
public static class HubErrors
{
    private static HubException Create(string code, string message)
        => new($"{code}|{message}");

    // ─── Auth ─────────────────────────────────────────────────────────────
    public static HubException Unauthorized()
        => Create(HubErrorCodes.Unauthorized, "Người dùng chưa được xác thực.");

    // ─── Send ─────────────────────────────────────────────────────────────
    public static HubException MessageSendFailed()
        => Create(HubErrorCodes.MessageSendFailed, "Không thể gửi tin nhắn.");

    public static HubException FileSendFailed()
        => Create(HubErrorCodes.FileSendFailed, "Không thể gửi file.");

    // ─── Edit ─────────────────────────────────────────────────────────────
    public static HubException InvalidRequest()
    => Create(
        HubErrorCodes.InvalidRequest,
        "Dữ liệu yêu cầu không hợp lệ."
    );
    public static HubException MessageNotFound()
        => Create(HubErrorCodes.MessageNotFound, "Không tìm thấy tin nhắn.");

    public static HubException MessageEditForbidden()
        => Create(HubErrorCodes.MessageEditForbidden, "Bạn không có quyền sửa tin nhắn này.");

    public static HubException MessageAlreadyDeleted()
        => Create(HubErrorCodes.MessageAlreadyDeleted, "Tin nhắn đã bị xóa.");

    public static HubException MessageEditFailed()
        => Create(HubErrorCodes.MessageEditFailed, "Không thể chỉnh sửa tin nhắn.");

    // ─── Delete ───────────────────────────────────────────────────────────
    public static HubException MessageDeleteForbidden()
        => Create(HubErrorCodes.MessageDeleteForbidden, "Bạn không có quyền xóa tin nhắn này.");

    public static HubException MessageDeleteFailed()
        => Create(HubErrorCodes.MessageDeleteFailed, "Không thể xóa tin nhắn.");

    // ─── Reply ────────────────────────────────────────────────────────────
    public static HubException ParentMessageNotFound()
        => Create(HubErrorCodes.ParentMessageNotFound, "Không tìm thấy tin nhắn gốc.");

    public static HubException MessageReplyFailed()
        => Create(HubErrorCodes.MessageReplyFailed, "Không thể reply tin nhắn.");

    // ─── Read ─────────────────────────────────────────────────────────────
    public static HubException MarkAsReadFailed()
        => Create(HubErrorCodes.MarkAsReadFailed, "Không thể đánh dấu đã đọc.");
        public static HubException MessageReplyForbidden ()
        => Create(HubErrorCodes.MessageReplyForbidden , "Không thể reply tin nhắn.");

    // ─── Read ─────────────────────────────────────────────────────────────
    public static HubException ParentMessageDeleted()
        => Create(HubErrorCodes.ParentMessageDeleted, "Không thể đánh dấu đã đọc.");
}