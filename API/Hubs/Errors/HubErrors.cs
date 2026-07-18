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
    public static HubException MessageReplyForbidden()
    => Create(HubErrorCodes.MessageReplyForbidden, "Không thể reply tin nhắn.");

    // ─── Read ─────────────────────────────────────────────────────────────
    public static HubException ParentMessageDeleted()
        => Create(HubErrorCodes.ParentMessageDeleted, "Không thể trả lời tin nhắn đã bị xóa.");

    // ─── Pin ──────────────────────────────────────────────────────────────
    public static HubException MessagePinForbidden()
        => Create(HubErrorCodes.MessagePinForbidden, "Bạn không có quyền ghim tin nhắn.");

    public static HubException MessagePinFailed()
        => Create(HubErrorCodes.MessagePinFailed, "Không thể ghim tin nhắn.");

    public static HubException MessageUnpinFailed()
        => Create(HubErrorCodes.MessageUnpinFailed, "Không thể bỏ ghim tin nhắn.");

    public static HubException MessageAlreadyPinned()
        => Create(HubErrorCodes.MessageAlreadyPinned, "Tin nhắn đã được ghim.");

    public static HubException MessageNotPinned()
        => Create(HubErrorCodes.MessageNotPinned, "Tin nhắn chưa được ghim.");

    // ─── Reaction ─────────────────────────────────────────────────────────
    public static HubException MessageReactionForbidden()
        => Create(HubErrorCodes.MessageReactionForbidden, "Bạn không có quyền thả cảm xúc tin nhắn này.");

    public static HubException MessageReactionFailed()
        => Create(HubErrorCodes.MessageReactionFailed, "Không thể cập nhật cảm xúc tin nhắn.");

    public static HubException MessageAlreadyReacted()
        => Create(HubErrorCodes.MessageAlreadyReacted, "Bạn đã thả cảm xúc này rồi.");

    // ─── Call ─────────────────────────────────────────────────────────────
    public static HubException CallNotFound()
        => Create(HubErrorCodes.CallNotFound, "Không tìm thấy cuộc gọi.");

    public static HubException CallInitFailed()
        => Create(HubErrorCodes.CallInitFailed, "Không thể khởi tạo cuộc gọi.");

    public static HubException CallAnswerFailed()
        => Create(HubErrorCodes.CallAnswerFailed, "Không thể trả lời cuộc gọi.");

    public static HubException CallRejectFailed()
        => Create(HubErrorCodes.CallRejectFailed, "Không thể từ chối cuộc gọi.");

    public static HubException CallEndFailed()
        => Create(HubErrorCodes.CallEndFailed, "Không thể kết thúc cuộc gọi.");

    public static HubException IceCandidateFailed()
        => Create(HubErrorCodes.IceCandidateFailed, "Không thể gửi ICE candidate.");

    public static HubException NotInChatroom()
        => Create(HubErrorCodes.NotInChatroom, "Bạn không phải thành viên chatroom này.");

    public static HubException CallJoinFailed()
        => Create(HubErrorCodes.CallJoinFailed, "Không thể tham gia cuộc gọi.");

    public static HubException CallLeaveFailed()
        => Create(HubErrorCodes.CallLeaveFailed, "Không thể rời cuộc gọi.");

    public static HubException CallSyncFailed()
        => Create(HubErrorCodes.CallSyncFailed, "Không thể đồng bộ trạng thái cuộc gọi.");

    public static HubException IceRestartFailed()
        => Create(HubErrorCodes.IceRestartFailed, "Không thể khởi động lại kết nối.");
}
