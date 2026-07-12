using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.API.Hubs.Errors;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Domain.Interfaces.Repositories;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.Domain.DTOs.Responses.Calls;
using linksy_backend_api.DTOs.MessagesDTOs;
using linksy_backend_api.Models;
using linksy_backend_api.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using linksy_backend_api.Domain.Entities.Models;

namespace linksy_backend_api.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IMessageService _messageService;
        private readonly IConnectionManager _connectionManager;
        private readonly IChatroomAccessService _chatroomAccessService;
        private readonly ICallService _callService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            IChatroomService chatService,
            IMessageService messageService,
            IConnectionManager connectionManager,
            IChatroomAccessService chatroomAccessService,
            ICallService callService,
            ILogger<ChatHub> logger)
        {
            _connectionManager = connectionManager;
            _chatroomAccessService = chatroomAccessService;
            _logger = logger;
            _messageService = messageService;
            _callService = callService;
        }
        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                await _connectionManager.AddConnectionAsync(userId, Context.ConnectionId);
                await Clients.Others.SendAsync("UserOnline", userId);
                _logger.LogInformation(
                    "User {UserId} connected: {ConnectionId}", userId, Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OnConnectedAsync failed for ConnectionId={ConnectionId}",
                    Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _connectionManager.RemoveConnectionAsync(userId, Context.ConnectionId);

                var hasConnections = await _connectionManager.HasConnectionsAsync(userId);
                if (!hasConnections)
                    await Clients.Others.SendAsync("UserOffline", userId);

                _logger.LogInformation("User {UserId} disconnected", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OnDisconnectedAsync failed for ConnectionId={ConnectionId}",
                    Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinChatroom(Guid chatroomId)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _chatroomAccessService.EnsureMemberAsync(chatroomId, userId);
                await Groups.AddToGroupAsync(Context.ConnectionId, chatroomId.ToString());

                _logger.LogInformation(
                    "User {UserId} joined chatroom {ChatroomId}", userId, chatroomId);

                await Clients.OthersInGroup(chatroomId.ToString())
                    .SendAsync("UserJoinedChatroom", new
                    {
                        UserId = userId,
                        ChatroomId = chatroomId,
                        Timestamp = DateTime.UtcNow
                    });
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining chatroom {ChatroomId}", chatroomId);
                throw new HubException("Không thể tham gia phòng chat.");
            }
        }
        // Leave chatroom
        public async Task LeaveChatroom(Guid chatroomId)
        {
            try
            {
                var userId = GetCurrentUserId();
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatroomId.ToString());

                _logger.LogInformation(
                    "User {UserId} left chatroom {ChatroomId}", userId, chatroomId);

                await Clients.OthersInGroup(chatroomId.ToString())
                    .SendAsync("UserLeftChatroom", new
                    {
                        UserId = userId,
                        ChatroomId = chatroomId,
                        Timestamp = DateTime.UtcNow
                    });
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving chatroom {ChatroomId}", chatroomId);
                throw new HubException("Không thể rời phòng chat.");
            }
        }

        // Gửi tin nhắn
        public async Task SendMessage(SendMessageRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                // Service xử lý broadcast với IsOwn đúng per-user
                await _messageService.SendMessageAsync(userId, request);
            }
            catch (HubException) { throw; }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized SendMessage");
                throw HubErrors.MessageSendFailed();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                throw HubErrors.MessageSendFailed();
            }
        }


        // Typing indicator
        public async Task StartTyping(Guid chatroomId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var username = Context.User?.FindFirst(
                    System.Security.Claims.ClaimTypes.Name)?.Value;

                await Clients.OthersInGroup(chatroomId.ToString())
                    .SendAsync("UserTyping", new
                    {
                        UserId = userId,
                        Username = username,
                        ChatroomId = chatroomId
                    });
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error StartTyping chatroom {ChatroomId}", chatroomId);
            }
        }

        public async Task StopTyping(Guid chatroomId)
        {
            try
            {
                var userId = GetCurrentUserId();
                await Clients.OthersInGroup(chatroomId.ToString())
                    .SendAsync("UserStoppedTyping", new
                    {
                        UserId = userId,
                        ChatroomId = chatroomId
                    });
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error StopTyping chatroom {ChatroomId}", chatroomId);
            }
        }

        // Đánh dấu đã đọc
        public async Task MarkAsRead(Guid chatroomId, Guid messageId)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _messageService.MarkMessageAsReadAsync(userId, chatroomId, messageId);
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error marking MessageId={MessageId} as read", messageId);
                throw HubErrors.MarkAsReadFailed();
            }
        }

        // Xóa tin nhắn
        public async Task DeleteMessage(Guid messageId)
        {
            Guid? userId = null;

            try
            {
                userId = GetCurrentUserId();
                await _messageService.DeleteMessageAsync(userId.Value, messageId);
            }
            catch (HubException)
            {
                _logger.LogWarning(
                    "Unauthenticated delete attempt. MessageId={MessageId}, ConnectionId={ConnectionId}",
                    messageId,
                    Context.ConnectionId
                );
                throw;
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning(
                    "Message not found while deleting. MessageId={MessageId}, UserId={UserId}",
                    messageId,
                    userId
                );
                throw HubErrors.MessageNotFound();
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning(
                    "Forbidden message delete. MessageId={MessageId}, UserId={UserId}",
                    messageId,
                    userId
                );
                throw HubErrors.MessageDeleteForbidden();
            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning(
                    "Attempted to delete an already deleted message. MessageId={MessageId}, UserId={UserId}",
                    messageId,
                    userId
                );
                throw HubErrors.MessageAlreadyDeleted();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error deleting message. MessageId={MessageId}, UserId={UserId}, ConnectionId={ConnectionId}",
                    messageId,
                    userId,
                    Context.ConnectionId
                );
                throw HubErrors.MessageDeleteFailed();
            }
        }

        // Chỉnh sửa tin nhắn
        public async Task EditMessage(Guid messageId, string newText)
        {
            Guid? userId = null;

            try
            {
                userId = GetCurrentUserId();

                await _messageService.EditMessageAsync(
                    userId.Value,
                    messageId,
                    newText
                );
            }
            catch (HubException)
            {
                _logger.LogWarning(
                    "Unauthenticated edit attempt. MessageId={MessageId}, ConnectionId={ConnectionId}",
                    messageId,
                    Context.ConnectionId
                );

                throw;
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning(
                    "Message not found while editing. MessageId={MessageId}, UserId={UserId}",
                    messageId,
                    userId
                );

                throw HubErrors.MessageNotFound();
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning(
                    "Forbidden message edit. MessageId={MessageId}, UserId={UserId}",
                    messageId,
                    userId
                );

                throw HubErrors.MessageEditForbidden();
            }
            catch (ArgumentException)
            {
                _logger.LogWarning(
                    "Invalid message edit request. MessageId={MessageId}, UserId={UserId}, TextLength={TextLength}",
                    messageId,
                    userId,
                    newText?.Length ?? 0
                );

                throw HubErrors.InvalidRequest();
            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning(
                    "Attempted to edit deleted message. MessageId={MessageId}, UserId={UserId}",
                    messageId,
                    userId
                );

                throw HubErrors.MessageAlreadyDeleted();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error editing message. MessageId={MessageId}, UserId={UserId}, ConnectionId={ConnectionId}",
                    messageId,
                    userId,
                    Context.ConnectionId
                );

                throw HubErrors.MessageEditFailed();
            }
        }

        // Reply to message
        public async Task ReplyToMessage(
    Guid chatroomId,
    Guid parentMessageId,
    string replyText)
        {
            Guid? userId = null;

            try
            {
                userId = GetCurrentUserId();

                await _messageService.SendMessageAsync(
                    userId.Value,
                    new SendMessageRequest
                    {
                        ChatroomId = chatroomId,
                        MessageType = "text",
                        MessageText = replyText,
                        ParentMessageId = parentMessageId
                    }
                );
            }
            catch (HubException) { throw; }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning(
                    "Parent message not found. ParentMessageId={ParentMessageId}, ChatroomId={ChatroomId}, UserId={UserId}",
                    parentMessageId, chatroomId, userId
                );

                throw HubErrors.ParentMessageNotFound();
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning(
                    "Forbidden message reply. ParentMessageId={ParentMessageId}, ChatroomId={ChatroomId}, UserId={UserId}",
                    parentMessageId, chatroomId, userId
                );

                throw HubErrors.MessageReplyForbidden();
            }
            catch (ArgumentException)
            {
                throw HubErrors.InvalidRequest();
            }
            catch (InvalidOperationException)
            {
                throw HubErrors.ParentMessageDeleted();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected reply error. ParentMessageId={ParentMessageId}, ChatroomId={ChatroomId}, UserId={UserId}, ConnectionId={ConnectionId}",
                    parentMessageId,
                    chatroomId,
                    userId,
                    Context.ConnectionId
                );

                throw HubErrors.MessageReplyFailed();
            }
        }

        // Gửi file
        public async Task SendFile(Guid chatroomId, string fileName, string fileUrl, string fileType)
        {
            try
            {
                var userId = GetCurrentUserId();
                var request = new SendMessageRequest
                {
                    ChatroomId = chatroomId,
                    MessageType = fileType, // "image" | "file" | "video" | "audio"
                    MessageText = fileUrl
                };
                await _messageService.SendMessageAsync(userId, request);
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending file to chatroom {ChatroomId}", chatroomId);
                throw HubErrors.FileSendFailed();
            }
        }

        // Gửi notification
        private async Task SendNotificationToUser(Guid recipientUserId, object notification)
        {
            try
            {
                var connections = await _connectionManager
                    .GetConnectionsAsync(recipientUserId);

                if (connections.Any())
                    await Clients.Clients(connections)
                        .SendAsync("ReceiveNotification", notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending notification to UserId={UserId}", recipientUserId);
            }
        }
        private Guid GetCurrentUserId()
        {
            var value = Context.User?.FindFirst("user_id")?.Value;
            if (!Guid.TryParse(value, out var userId))
                throw HubErrors.Unauthorized();
            return userId;
        }

        // Video/Voice call 

#if false // Replaced below: legacy call signaling trusted caller-supplied identifiers.
        public async Task InitiateCall(Guid chatroomId, string callType, string sdpOffer)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Kiểm tra quyền truy cập chatroom
                await _chatroomAccessService.EnsureMemberAsync(chatroomId, userId);

                // Lưu CallLog vào DB, trả về callLogId
                var callLogId = await _callService.InitiateCallAsync(
                    callerId: userId,
                    chatroomId: chatroomId,
                    callType: callType   // "video" | "audio"
                );

                // Broadcast tới các member khác trong chatroom
                await Clients.OthersInGroup(chatroomId.ToString())
                    .SendAsync("IncomingCall", new
                    {
                        CallLogId = callLogId,
                        CallerId = userId,
                        ChatroomId = chatroomId,
                        CallType = callType,
                        SdpOffer = sdpOffer
                    });

                _logger.LogInformation(
                    "User {UserId} initiated {CallType} call in chatroom {ChatroomId}, CallLogId={CallLogId}",
                    userId, callType, chatroomId, callLogId);
            }
            catch (HubException) { throw; }
            catch (UnauthorizedAccessException)
            {
                throw HubErrors.NotInChatroom();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating call in chatroom {ChatroomId}", chatroomId);
                throw HubErrors.CallInitFailed();
            }
        }
        public async Task CallUser(string recipientUserId, string chatroomId, string callType, object offer)
        {
            var userId = Context.User?.FindFirst("user_id")?.Value;
            var username = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            var connections = await _connectionManager.GetConnectionsAsync(Guid.Parse(recipientUserId));

            if (connections.Any())
            {
                await Clients.Clients(connections).SendAsync("IncomingCall", new
                {
                    CallerId = userId,
                    CallerName = username,
                    ChatroomId = chatroomId,
                    CallType = callType, // "video" or "voice"
                    Offer = offer
                });
            }
        }

        public async Task AnswerCall(Guid callLogId, Guid callerId, string sdpAnswer)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Cập nhật DB: status -> "answered", AnsweredAt = now
                await _callService.AnswerCallAsync(callLogId, userId);

                // Gửi Answer về đúng Caller
                var callerConnections = await _connectionManager.GetConnectionsAsync(callerId);
                if (callerConnections.Any())
                {
                    await Clients.Clients(callerConnections)
                        .SendAsync("CallAnswered", new
                        {
                            CallLogId = callLogId,
                            SdpAnswer = sdpAnswer
                        });
                }

                _logger.LogInformation(
                    "User {UserId} answered call {CallLogId}", userId, callLogId);
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error answering call {CallLogId}", callLogId);
                throw HubErrors.CallAnswerFailed();
            }
        }

        public async Task RejectCall(Guid callLogId, Guid callerId)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Cập nhật DB: status -> "rejected"
                await _callService.RejectCallAsync(callLogId, userId);

                var callerConnections = await _connectionManager.GetConnectionsAsync(callerId);
                if (callerConnections.Any())
                {
                    await Clients.Clients(callerConnections)
                        .SendAsync("CallRejected", new { CallLogId = callLogId });
                }

                _logger.LogInformation(
                    "User {UserId} rejected call {CallLogId}", userId, callLogId);
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting call {CallLogId}", callLogId);
                throw HubErrors.CallEndFailed();
            }
        }
        public async Task EndCall(Guid callLogId, Guid chatroomId)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Cập nhật DB: EndedAt, DurationSec
                await _callService.EndCallAsync(callLogId, userId);

                // Broadcast cho cả chatroom group
                await Clients.OthersInGroup(chatroomId.ToString())
                    .SendAsync("CallEnded", new
                    {
                        CallLogId = callLogId,
                        EndedBy = userId,
                        Timestamp = DateTime.UtcNow
                    });

                _logger.LogInformation(
                    "User {UserId} ended call {CallLogId}", userId, callLogId);
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending call {CallLogId}", callLogId);
                throw HubErrors.CallEndFailed();
            }
        }


        public async Task SendIceCandidate(string recipientUserId, object candidate)
        {
            try
            {
                var connections = await _connectionManager.GetConnectionsAsync(targetUserId);
                if (connections.Any())
                {
                    await Clients.Clients(connections)
                        .SendAsync("IceCandidate", new
                        {
                            CallLogId = callLogId,
                            FromUserId = GetCurrentUserId(),
                            CandidateJson = candidateJson
                        });
                }
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending ICE candidate to {TargetUserId}", targetUserId);
                throw HubErrors.IceCandidateFailed();
            }
        }
#endif

        // WebRTC signaling. Media itself is exchanged directly by clients; the
        // hub persists call state and forwards SDP/ICE only.
        public async Task InitiateCall(Guid chatroomId, string callType, string sdpOffer)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sdpOffer))
                    throw HubErrors.InvalidRequest();

                var callerId = GetCurrentUserId();
                var call = await _callService.InitiateCallAsync(callerId, chatroomId, callType);
                var recipientId = call.Participants
                    .Single(participant => participant.UserId != callerId)
                    .UserId;
                var connections = await _connectionManager.GetConnectionsAsync(recipientId);

                if (!connections.Any())
                {
                    await _callService.EndCallAsync(call.Id, callerId);
                    await Clients.Caller.SendAsync("CallFailed", new
                    {
                        CallLogId = call.Id,
                        Reason = "recipient_offline"
                    });
                    return;
                }

                // Gửi CallInitiated cho Caller TRƯỚC để Caller có callLogId
                // trước khi Callee có thể trả lời → tránh race condition CallAnswered đến trước CallInitiated
                await Clients.Caller.SendAsync("CallInitiated", CallLogDto.FromEntity(call));

                await Clients.Clients(connections).SendAsync("IncomingCall", new
                {
                    CallLogId = call.Id,
                    CallerId = callerId,
                    ChatroomId = call.ChatroomId,
                    CallType = call.CallType,
                    SdpOffer = sdpOffer
                });

                _logger.LogInformation(
                    "User {UserId} initiated {CallType} call {CallLogId}",
                    callerId,
                    call.CallType,
                    call.Id);
            }
            catch (HubException) { throw; }
            catch (UnauthorizedAccessException)
            {
                throw HubErrors.NotInChatroom();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating call in chatroom {ChatroomId}", chatroomId);
                throw HubErrors.CallInitFailed();
            }
        }

        public async Task AnswerCall(Guid callLogId, string sdpAnswer)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sdpAnswer))
                    throw HubErrors.InvalidRequest();

                var userId = GetCurrentUserId();
                var call = await _callService.AnswerCallAsync(callLogId, userId);
                var connections = await _connectionManager.GetConnectionsAsync(call.CallerId);

                if (connections.Any())
                {
                    await Clients.Clients(connections).SendAsync("CallAnswered", new
                    {
                        Call = CallLogDto.FromEntity(call),
                        AnsweredBy = userId,
                        SdpAnswer = sdpAnswer
                    });
                }
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error answering call {CallLogId}", callLogId);
                throw HubErrors.CallAnswerFailed();
            }
        }

        public async Task RejectCall(Guid callLogId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var call = await _callService.RejectCallAsync(callLogId, userId);
                var connections = await _connectionManager.GetConnectionsAsync(call.CallerId);

                if (connections.Any())
                {
                    await Clients.Clients(connections).SendAsync("CallRejected", new
                    {
                        Call = CallLogDto.FromEntity(call),
                        RejectedBy = userId
                    });
                }
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting call {CallLogId}", callLogId);
                throw HubErrors.CallRejectFailed();
            }
        }

        public async Task EndCall(Guid callLogId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var call = await _callService.EndCallAsync(callLogId, userId);

                foreach (var recipientId in call.Participants
                             .Select(participant => participant.UserId)
                             .Distinct())
                {
                    var connections = await _connectionManager.GetConnectionsAsync(recipientId);
                    if (!connections.Any())
                    continue;
                    
                    await Clients.Clients(connections).SendAsync("CallEnded", new
                    {
                        Call = CallLogDto.FromEntity(call),
                        EndedBy = userId,
                    });
                }
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending call {CallLogId}", callLogId);
                throw HubErrors.CallEndFailed();
            }
        }

        public async Task SendIceCandidate(
            Guid callLogId,
            Guid recipientUserId,
            string candidateJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(candidateJson))
                    throw HubErrors.InvalidRequest();

                var userId = GetCurrentUserId();
                await _callService.EnsureCanSignalAsync(callLogId, userId, recipientUserId);
                var connections = await _connectionManager.GetConnectionsAsync(recipientUserId);

                if (connections.Any())
                {
                    await Clients.Clients(connections).SendAsync("IceCandidate", new
                    {
                        CallLogId = callLogId,
                        FromUserId = userId,
                        CandidateJson = candidateJson
                    });
                }
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending ICE candidate for call {CallLogId}", callLogId);
                throw HubErrors.IceCandidateFailed();
            }
        }
    }
}
