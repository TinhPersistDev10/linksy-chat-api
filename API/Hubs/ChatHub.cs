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
using linksy_backend_api.Domain.DTOs.Requests.Reacions;

namespace linksy_backend_api.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IMessageService _messageService;
        private readonly IReactionService _reactionService;
        private readonly IPollService _pollService;
        private readonly IConnectionManager _connectionManager;
        private readonly IChatroomAccessService _chatroomAccessService;
        private readonly ICallService _callService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            IChatroomService chatService,
            IMessageService messageService,
            IReactionService reactionService,
            IPollService pollService,
            IConnectionManager connectionManager,
            IChatroomAccessService chatroomAccessService,
            ICallService callService,
            ILogger<ChatHub> logger)
        {
            _connectionManager = connectionManager;
            _chatroomAccessService = chatroomAccessService;
            _logger = logger;
            _messageService = messageService;
            _reactionService = reactionService;
            _pollService = pollService;
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
                {
                    await Clients.Others.SendAsync("UserOffline", userId);
                    await HandleDisconnectedCallsAsync(userId);
                }

                _logger.LogInformation("User {UserId} disconnected", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OnDisconnectedAsync failed for ConnectionId={ConnectionId}",
                    Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        private async Task HandleDisconnectedCallsAsync(Guid userId)
        {
            var changedCalls = await _callService.HandleParticipantDisconnectedAsync(userId);

            foreach (var call in changedCalls)
            {
                var dto = CallLogDto.FromEntity(call);
                if (call.Status == "ended")
                {
                    foreach (var participantId in OtherParticipantIds(call, userId))
                    {
                        var connections = await _connectionManager.GetConnectionsAsync(participantId);
                        if (!connections.Any()) continue;

                        await Clients.Clients(connections).SendAsync("CallEnded", new
                        {
                            Call = dto,
                            EndedBy = userId
                        });
                    }

                    await CreateCallSummaryMessageAsync(call);
                    continue;
                }

                foreach (var participantId in OtherParticipantIds(call, userId))
                {
                    var connections = await _connectionManager.GetConnectionsAsync(participantId);
                    if (!connections.Any()) continue;

                    await Clients.Clients(connections).SendAsync("CallParticipantLeft", new
                    {
                        Call = dto,
                        LeftBy = userId
                    });
                }
            }
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

        public async Task PinMessage(Guid messageId)
        {
            Guid? userId = null;
            try
            {
                userId = GetCurrentUserId();
                await _messageService.PinMessageAsync(userId.Value, messageId);
            }
            catch (HubException) { throw; }
            catch (KeyNotFoundException)
            {
                throw HubErrors.MessageNotFound();
            }
            catch (UnauthorizedAccessException)
            {
                throw HubErrors.MessagePinForbidden();
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("đã được ghim", StringComparison.OrdinalIgnoreCase))
            {
                throw HubErrors.MessageAlreadyPinned();
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("đã xóa", StringComparison.OrdinalIgnoreCase))
            {
                throw HubErrors.MessageAlreadyDeleted();
            }
            catch (InvalidOperationException)
            {
                throw HubErrors.MessagePinFailed();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pinning MessageId={MessageId}, UserId={UserId}", messageId, userId);
                throw HubErrors.MessagePinFailed();
            }
        }

        public async Task UnpinMessage(Guid messageId)
        {
            Guid? userId = null;
            try
            {
                userId = GetCurrentUserId();
                await _messageService.UnpinMessageAsync(userId.Value, messageId);
            }
            catch (HubException) { throw; }
            catch (KeyNotFoundException)
            {
                throw HubErrors.MessageNotPinned();
            }
            catch (UnauthorizedAccessException)
            {
                throw HubErrors.MessagePinForbidden();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unpinning MessageId={MessageId}, UserId={UserId}", messageId, userId);
                throw HubErrors.MessageUnpinFailed();
            }
        }
        public async Task ToggleReaction(Guid messageId, string emojiCode)
        {
            Guid? userId = null;
            try
            {
                userId = GetCurrentUserId();
                await _reactionService.ToggleReactionAsync(
                    userId.Value,
                    messageId,
                    new ToggleReactionRequest { EmojiCode = emojiCode });
            }
            catch (HubException) { throw; }
            catch (KeyNotFoundException) { throw HubErrors.MessageNotFound(); }
            catch (UnauthorizedAccessException) { throw HubErrors.MessageReactionForbidden(); }
            catch (InvalidOperationException) { throw HubErrors.MessageAlreadyDeleted(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling reaction for MessageId={MessageId}, UserId={UserId}", messageId, userId);
                throw HubErrors.MessageReactionFailed();
            }
        }

        public async Task VotePoll(Guid messageId, Guid optionId)
        {
            Guid? userId = null;
            try
            {
                userId = GetCurrentUserId();
                await _pollService.VoteAsync(
                    userId.Value,
                    messageId,
                    new VotePollRequest { OptionId = optionId });
            }
            catch (HubException) { throw; }
            catch (KeyNotFoundException) { throw HubErrors.MessageNotFound(); }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized VotePoll MessageId={MessageId}", messageId);
                throw new HubException("Bạn không thể bình chọn.");
            }
            catch (InvalidOperationException ex)
            {
                throw new HubException(ex.Message);
            }
            catch (ArgumentException ex)
            {
                throw new HubException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error voting poll MessageId={MessageId}, UserId={UserId}", messageId, userId);
                throw new HubException("Không thể bình chọn.");
            }
        }

        public async Task ClosePoll(Guid messageId)
        {
            Guid? userId = null;
            try
            {
                userId = GetCurrentUserId();
                await _pollService.CloseAsync(userId.Value, messageId);
            }
            catch (HubException) { throw; }
            catch (KeyNotFoundException) { throw HubErrors.MessageNotFound(); }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized ClosePoll MessageId={MessageId}", messageId);
                throw new HubException("Bạn không có quyền đóng bình chọn.");
            }
            catch (InvalidOperationException ex)
            {
                throw new HubException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing poll MessageId={MessageId}, UserId={UserId}", messageId, userId);
                throw new HubException("Không thể đóng bình chọn.");
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

        // WebRTC signaling. Media itself is exchanged directly by clients; the
        // hub persists call state and forwards SDP/ICE only.
        //
        // Mesh model: InitiateCall invites every active chatroom member (1 for
        // direct chats, N for group chats). Each invited participant answers
        // independently via AnswerCall/JoinCall, and SendCallOffer/SendCallAnswer
        // are used for the additional per-peer SDP exchanges required once more
        // than two participants are in the same call (every pair needs its own
        // RTCPeerConnection on the client).
        public async Task InitiateCall(Guid chatroomId, string callType, string sdpOffer)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sdpOffer))
                    throw HubErrors.InvalidRequest();

                var callerId = GetCurrentUserId();
                var call = await _callService.InitiateCallAsync(callerId, chatroomId, callType);
                var recipientIds = call.Participants
                    .Where(participant => participant.UserId != callerId)
                    .Select(participant => participant.UserId)
                    .ToList();

                var reachableConnections = new List<string>();
                foreach (var recipientId in recipientIds)
                {
                    var connections = await _connectionManager.GetConnectionsAsync(recipientId);
                    reachableConnections.AddRange(connections);
                }

                if (reachableConnections.Count == 0)
                {
                    var endedCall = await _callService.EndCallAsync(call.Id, callerId);
                    await CreateCallSummaryMessageAsync(endedCall);
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

                await Clients.Clients(reachableConnections).SendAsync("IncomingCall", new
                {
                    CallLogId = call.Id,
                    CallerId = callerId,
                    ChatroomId = call.ChatroomId,
                    CallType = call.CallType,
                    SdpOffer = sdpOffer
                });

                _logger.LogInformation(
                    "User {UserId} initiated {CallType} call {CallLogId} with {RecipientCount} recipient(s)",
                    callerId,
                    call.CallType,
                    call.Id,
                    recipientIds.Count);
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

        public async Task StartGroupCall(Guid chatroomId, string callType)
        {
            try
            {
                var callerId = GetCurrentUserId();
                var call = await _callService.InitiateCallAsync(callerId, chatroomId, callType);
                var recipientIds = call.Participants
                    .Where(participant => participant.UserId != callerId)
                    .Select(participant => participant.UserId)
                    .ToList();

                var reachableConnections = new List<string>();
                foreach (var recipientId in recipientIds)
                {
                    var connections = await _connectionManager.GetConnectionsAsync(recipientId);
                    reachableConnections.AddRange(connections);
                }

                if (reachableConnections.Count == 0)
                {
                    var endedCall = await _callService.EndCallAsync(call.Id, callerId);
                    await CreateCallSummaryMessageAsync(endedCall);
                    await Clients.Caller.SendAsync("CallFailed", new
                    {
                        CallLogId = call.Id,
                        Reason = "recipient_offline"
                    });
                    return;
                }

                var dto = CallLogDto.FromEntity(call);
                await Clients.Caller.SendAsync("GroupCallStarted", dto);
                await Clients.Clients(reachableConnections).SendAsync("IncomingGroupCall", new
                {
                    Call = dto,
                    CallerId = callerId,
                    ChatroomId = call.ChatroomId,
                    CallType = call.CallType
                });
            }
            catch (HubException) { throw; }
            catch (UnauthorizedAccessException)
            {
                throw HubErrors.NotInChatroom();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting group call in chatroom {ChatroomId}", chatroomId);
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

                foreach (var recipientId in OtherParticipantIds(call, userId))
                {
                    var connections = await _connectionManager.GetConnectionsAsync(recipientId);
                    if (!connections.Any()) continue;

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

                foreach (var recipientId in OtherParticipantIds(call, userId))
                {
                    var connections = await _connectionManager.GetConnectionsAsync(recipientId);
                    if (!connections.Any()) continue;

                    await Clients.Clients(connections).SendAsync("CallRejected", new
                    {
                        Call = CallLogDto.FromEntity(call),
                        RejectedBy = userId
                    });
                }

                if (call.Status is "rejected" or "ended")
                    await CreateCallSummaryMessageAsync(call);
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

                foreach (var recipientId in OtherParticipantIds(call, userId))
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

                await CreateCallSummaryMessageAsync(call);
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending call {CallLogId}", callLogId);
                throw HubErrors.CallEndFailed();
            }
        }

        /// <summary>
        /// Explicit join for participants added to an already-active call (group
        /// scenario) without going through the initial IncomingCall/AnswerCall
        /// handshake, e.g. a participant who missed the initial ring rejoining.
        /// </summary>
        public async Task<CallLogDto> JoinCall(Guid callLogId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var call = await _callService.JoinCallAsync(callLogId, userId);
                var dto = CallLogDto.FromEntity(call);

                foreach (var participantId in OtherParticipantIds(call, userId))
                {
                    var connections = await _connectionManager.GetConnectionsAsync(participantId);
                    if (!connections.Any()) continue;

                    await Clients.Clients(connections).SendAsync("CallParticipantJoined", new
                    {
                        Call = dto,
                        JoinedBy = userId
                    });
                }

                // Return the fresh call state (including AnsweredAt) so the joining
                // client can align its call timer with the shared server timestamp.
                return dto;
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining call {CallLogId}", callLogId);
                throw HubErrors.CallJoinFailed();
            }
        }

        /// <summary>
        /// Leaves a call without ending it for the remaining participants
        /// (group scenario). If the departing participant was the last one
        /// still joined, the call service marks the whole call as ended and we
        /// persist the usual call-log message.
        /// </summary>
        public async Task LeaveCall(Guid callLogId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var call = await _callService.LeaveCallAsync(callLogId, userId);
                var dto = CallLogDto.FromEntity(call);

                foreach (var participantId in OtherParticipantIds(call, userId))
                {
                    var connections = await _connectionManager.GetConnectionsAsync(participantId);
                    if (!connections.Any()) continue;

                    await Clients.Clients(connections).SendAsync("CallParticipantLeft", new
                    {
                        Call = dto,
                        LeftBy = userId
                    });
                }

                if (call.Status == "ended")
                    await CreateCallSummaryMessageAsync(call);
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving call {CallLogId}", callLogId);
                throw HubErrors.CallLeaveFailed();
            }
        }

        /// <summary>
        /// Lets a reconnecting/refreshed client discover whether its current
        /// chatroom already has an active call so it can rejoin instead of
        /// showing a stale UI.
        /// </summary>
        public async Task<CallLogDto?> SyncCallState(Guid chatroomId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var call = await _callService.GetActiveCallForChatroomAsync(chatroomId, userId);
                return call is null ? null : CallLogDto.FromEntity(call);
            }
            catch (HubException) { throw; }
            catch (UnauthorizedAccessException)
            {
                throw HubErrors.NotInChatroom();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing call state for chatroom {ChatroomId}", chatroomId);
                throw HubErrors.CallSyncFailed();
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

        /// <summary>
        /// Per-peer SDP offer, used when a client needs to negotiate directly
        /// with one specific participant (e.g. a new joiner establishing mesh
        /// connections with everyone already in a group call).
        /// </summary>
        public async Task SendCallOffer(Guid callLogId, Guid recipientUserId, string sdpOffer)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sdpOffer))
                    throw HubErrors.InvalidRequest();

                var userId = GetCurrentUserId();
                await _callService.EnsureCanSignalAsync(callLogId, userId, recipientUserId);
                var connections = await _connectionManager.GetConnectionsAsync(recipientUserId);

                if (connections.Any())
                {
                    await Clients.Clients(connections).SendAsync("CallOffer", new
                    {
                        CallLogId = callLogId,
                        FromUserId = userId,
                        SdpOffer = sdpOffer
                    });
                }
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending call offer for call {CallLogId}", callLogId);
                throw HubErrors.CallAnswerFailed();
            }
        }

        /// <summary>Per-peer SDP answer counterpart to <see cref="SendCallOffer"/>.</summary>
        public async Task SendCallAnswer(Guid callLogId, Guid recipientUserId, string sdpAnswer)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sdpAnswer))
                    throw HubErrors.InvalidRequest();

                var userId = GetCurrentUserId();
                await _callService.EnsureCanSignalAsync(callLogId, userId, recipientUserId);
                var connections = await _connectionManager.GetConnectionsAsync(recipientUserId);

                if (connections.Any())
                {
                    await Clients.Clients(connections).SendAsync("CallAnswer", new
                    {
                        CallLogId = callLogId,
                        FromUserId = userId,
                        SdpAnswer = sdpAnswer
                    });
                }
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending call answer for call {CallLogId}", callLogId);
                throw HubErrors.CallAnswerFailed();
            }
        }

        /// <summary>
        /// Asks one specific peer to perform an ICE restart (e.g. after a
        /// network interruption) instead of tearing down the whole call.
        /// </summary>
        public async Task RestartIce(Guid callLogId, Guid recipientUserId)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _callService.EnsureCanSignalAsync(callLogId, userId, recipientUserId);
                var connections = await _connectionManager.GetConnectionsAsync(recipientUserId);

                if (connections.Any())
                {
                    await Clients.Clients(connections).SendAsync("IceRestartRequested", new
                    {
                        CallLogId = callLogId,
                        FromUserId = userId
                    });
                }
            }
            catch (HubException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting ICE restart for call {CallLogId}", callLogId);
                throw HubErrors.IceRestartFailed();
            }
        }

        private static IEnumerable<Guid> OtherParticipantIds(CallLog call, Guid excludeUserId) =>
            call.Participants
                .Select(participant => participant.UserId)
                .Distinct()
                .Where(participantId => participantId != excludeUserId);

        /// <summary>
        /// Persists the finished call as a real chat message so it survives
        /// reload/tab switches. Best-effort: a failure here must never fail the
        /// call-end flow the user is already waiting on.
        /// </summary>
        private async Task CreateCallSummaryMessageAsync(CallLog call)
        {
            try
            {
                var (status, durationSec) = DeriveCallSummary(call);
                await _messageService.CreateCallLogMessageAsync(
                    call.ChatroomId,
                    call.CallerId,
                    call.Id,
                    call.CallType,
                    status,
                    durationSec,
                    call.StartedAt,
                    call.EndedAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to persist call log message for CallLogId={CallLogId}", call.Id);
            }
        }

        private static (string Status, int DurationSec) DeriveCallSummary(CallLog call)
        {
            if (call.Status == "rejected")
                return ("rejected", 0);

            if (call.AnsweredAt is null)
            {
                var invitedParticipants = call.Participants
                    .Where(participant => participant.UserId != call.CallerId)
                    .ToList();

                if (invitedParticipants.Count > 0 &&
                    invitedParticipants.All(participant => participant.Status == "declined"))
                {
                    return ("rejected", 0);
                }

                return ("missed", 0);
            }

            return ("ended", Math.Max(0, call.DurationSec ?? 0));
        }
    }
}
