using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.API.Hubs.Errors;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.DTOs.MessagesDTOs;
using linksy_backend_api.Models;
using linksy_backend_api.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace linksy_backend_api.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatroomService _chatService;
        private readonly IMessageService _messageService;
        private readonly IConnectionManager _connectionManager;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IChatroomService chatService, IMessageService messageService, IConnectionManager connectionManager, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _connectionManager = connectionManager;
            _logger = logger;
            _messageService = messageService;
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
        public async Task SendNotificationToUser(Guid recipientUserId, object notification)
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

        // Video/Voice call signaling
        // public async Task CallUser(string recipientUserId, string chatroomId, string callType, object offer)
        // {
        //     var userId = Context.User?.FindFirst("user_id")?.Value;
        //     var username = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        //     var connections = await _connectionManager.GetConnectionsAsync(Guid.Parse(recipientUserId));

        //     if (connections.Any())
        //     {
        //         await Clients.Clients(connections).SendAsync("IncomingCall", new
        //         {
        //             CallerId = userId,
        //             CallerName = username,
        //             ChatroomId = chatroomId,
        //             CallType = callType, // "video" or "voice"
        //             Offer = offer
        //         });
        //     }
        // }

        // public async Task AnswerCall(string callerUserId, object answer)
        // {
        //     var connections = await _connectionManager.GetConnectionsAsync(Guid.Parse(callerUserId));

        //     if (connections.Any())
        //     {
        //         await Clients.Clients(connections).SendAsync("CallAnswered", answer);
        //     }
        // }

        // public async Task RejectCall(string callerUserId)
        // {
        //     var connections = await _connectionManager.GetConnectionsAsync(Guid.Parse(callerUserId));

        //     if (connections.Any())
        //     {
        //         await Clients.Clients(connections).SendAsync("CallRejected");
        //     }
        // }

        // public async Task EndCall(string otherUserId)
        // {
        //     var connections = await _connectionManager.GetConnectionsAsync(Guid.Parse(otherUserId));

        //     if (connections.Any())
        //     {
        //         await Clients.Clients(connections).SendAsync("CallEnded");
        //     }
        // }

        // public async Task SendIceCandidate(string recipientUserId, object candidate)
        // {
        //     var connections = await _connectionManager.GetConnectionsAsync(Guid.Parse(recipientUserId));

        //     if (connections.Any())
        //     {
        //         await Clients.Clients(connections).SendAsync("IceCandidate", candidate);
        //     }
        // }
    }
}
