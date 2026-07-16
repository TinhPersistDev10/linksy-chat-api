namespace linksy_backend_api.API.Hubs.Errors;

public static class HubErrorCodes
{
    public const string Unauthorized = "UNAUTHORIZED";
    public const string InvalidRequest = "INVALID_REQUEST";
    public const string MessageNotFound = "MESSAGE_NOT_FOUND";
    public const string MessageEditForbidden = "MESSAGE_EDIT_FORBIDDEN";
    public const string MessageDeleteForbidden = "MESSAGE_DELETE_FORBIDDEN";
    public const string MessageAlreadyDeleted = "MESSAGE_ALREADY_DELETED";
    public const string MessageEditFailed = "MESSAGE_EDIT_FAILED";
    public const string MessageSendFailed = "MESSAGE_SEND_FAILED";
    public const string MessageDeleteFailed = "MESSAGE_DELETE_FAILED";
    public const string MessageReplyFailed = "MESSAGE_REPLY_FAILED";
    public const string ParentMessageNotFound = "PARENT_MESSAGE_NOT_FOUND";
    public const string FileSendFailed = "FILE_SEND_FAILED";
    public const string MarkAsReadFailed = "MARK_AS_READ_FAILED";
    public const string MessageReplyForbidden = "MESSAGE_REPLY_FORBIDDEN";
    public const string ParentMessageDeleted = "PARENT_MESSAGE_DELETED";

    //Call
    // Thêm vào cuối HubErrorCodes.cs
    public const string CallNotFound = "CALL_NOT_FOUND";
    public const string CallInitFailed = "CALL_INIT_FAILED";
    public const string CallAnswerFailed = "CALL_ANSWER_FAILED";
    public const string CallRejectFailed = "CALL_REJECT_FAILED";
    public const string CallEndFailed = "CALL_END_FAILED";
    public const string IceCandidateFailed = "ICE_CANDIDATE_FAILED";
    public const string NotInChatroom = "NOT_IN_CHATROOM";
    public const string CallJoinFailed = "CALL_JOIN_FAILED";
    public const string CallLeaveFailed = "CALL_LEAVE_FAILED";
    public const string CallSyncFailed = "CALL_SYNC_FAILED";
    public const string IceRestartFailed = "ICE_RESTART_FAILED";
}
