using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.DTOs.MessagesDTOs;
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
            var userId = Context.User?.FindFirst("user_id")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var userGuid = Guid.Parse(userId);
                await _connectionManager.AddConnectionAsync(userGuid, Context.ConnectionId);

                // Notify friends that user is online
                await Clients.Others.SendAsync("UserOnline", userId);

                _logger.LogInformation($"User {userId} connected with connection ID: {Context.ConnectionId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst("user_id")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var userGuid = Guid.Parse(userId);
                await _connectionManager.RemoveConnectionAsync(userGuid, Context.ConnectionId);

                // Check if user has any other active connections
                var hasConnections = await _connectionManager.HasConnectionsAsync(userGuid);
                if (!hasConnections)
                {
                    // Notify friends that user is offline
                    await Clients.Others.SendAsync("UserOffline", userId);
                }
                _logger.LogInformation($"User {userId} disconnected");
            }

            await base.OnDisconnectedAsync(exception);
        }
        public async Task JoinChatroom(string chatroomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, chatroomId);

            var userId = Context.User?.FindFirst("user_id")?.Value;
            _logger.LogInformation($"User {userId} joined chatroom {chatroomId}");

            // Notify other members
            await Clients.OthersInGroup(chatroomId).SendAsync("UserJoinedChatroom", new
            {
                UserId = userId,
                ChatroomId = chatroomId,
                Timestamp = DateTime.UtcNow
            });
        }
        // Leave chatroom
        public async Task LeaveChatroom(string chatroomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatroomId);

            var userId = Context.User?.FindFirst("user_id")?.Value;
            _logger.LogInformation($"User {userId} left chatroom {chatroomId}");

            // Notify other members
            await Clients.OthersInGroup(chatroomId).SendAsync("UserLeftChatroom", new
            {
                UserId = userId,
                ChatroomId = chatroomId,
                Timestamp = DateTime.UtcNow
            });
        }

        // Gửi tin nhắn
        public async Task SendMessage(SendMessageRequest messageDto)
        {
            try
            {
                var userIdClaim = Context.User?.FindFirst("user_id");
                if (userIdClaim == null)
                {
                    await Clients.Caller.SendAsync("Error", new { message = "User ID not found in claims" });
                    return;
                }
                if (!Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    await Clients.Caller.SendAsync("Error", new { message = "Invalid User ID format" });
                    return;
                }

                // Lưu message vào database
                var message = await _messageService.SendMessageAsync(userId, messageDto);

                // TẠO RESPONSE CHO NGƯỜI KHÁC (IsOwn = false)
                var responseForOthers = new
                {
                    MessageId = message.MessageId,
                    ChatroomId = message.ChatroomId,
                    SenderId = message.SenderId,
                    SenderUsername = message.SenderUsername,
                    SenderAvatar = message.SenderAvatar,
                    MessageType = message.MessageType,
                    MessageText = message.MessageText,
                    SentAt = message.SentAt,
                    IsOwn = false // Người khác nhận
                };


                var responseForSender = new
                {
                    MessageId = message.MessageId,
                    ChatroomId = message.ChatroomId,
                    SenderId = message.SenderId,
                    SenderUsername = message.SenderUsername,
                    SenderAvatar = message.SenderAvatar,
                    MessageType = message.MessageType,
                    MessageText = message.MessageText,
                    SentAt = message.SentAt,
                    IsOwn = true // Người gửi nhận
                };

                // GỬI CHO NGƯỜI KHÁC (không bao gồm người gửi)
                await Clients.OthersInGroup(messageDto.ChatroomId.ToString())
                    .SendAsync("ReceiveMessage", responseForOthers);


                await Clients.Caller
                    .SendAsync("ReceiveMessage", responseForSender);

                _logger.LogInformation($"Message sent by {userId} to chatroom {messageDto.ChatroomId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                await Clients.Caller.SendAsync("Error", new { message = ex.Message });
            }
        }

        // Typing indicator
        public async Task StartTyping(string chatroomId)
        {
            var userId = Context.User?.FindFirst("user_id")?.Value;
            var username = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            await Clients.OthersInGroup(chatroomId).SendAsync("UserTyping", new
            {
                UserId = userId,
                Username = username,
                ChatroomId = chatroomId
            });
        }

        public async Task StopTyping(string chatroomId)
        {
            var userId = Context.User?.FindFirst("user_id")?.Value;

            await Clients.OthersInGroup(chatroomId).SendAsync("UserStoppedTyping", new
            {
                UserId = userId,
                ChatroomId = chatroomId
            });
        }

        // Đánh dấu đã đọc
        public async Task MarkAsRead(string chatroomId, string messageId)
        {
            try
            {
                var userIdClaim = Context.User?.FindFirst("user_id");
                if (userIdClaim == null)
                {
                    await Clients.Caller.SendAsync("Error", new { message = "User ID not found in claims" });
                    return;
                }
                if (!Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    await Clients.Caller.SendAsync("Error", new { message = "Invalid User ID format" });
                    return;
                }

                // Notify sender that message was read
                await Clients.Group(chatroomId).SendAsync("MessageRead", new
                {
                    MessageId = messageId,
                    ReadBy = userId,
                    ReadAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message as read");
            }
        }

        // Xóa tin nhắn
        public async Task DeleteMessage(string chatroomId, string messageId)
        {
            try
            {
                var userIdClaim = Context.User?.FindFirst("user_id");
                if (userIdClaim == null)
                {
                    await Clients.Caller.SendAsync("Error", new { message = "User ID not found in claims" });
                    return;
                }
                if (!Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    await Clients.Caller.SendAsync("Error", new { message = "Invalid User ID format" });
                    return;
                }
                await _messageService.DeleteMessageAsync(userId, Guid.Parse(messageId));

                // Notify all members
                await Clients.Group(chatroomId).SendAsync("MessageDeleted", new
                {
                    MessageId = messageId,
                    ChatroomId = chatroomId,
                    DeletedBy = userId,
                    DeletedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message");
                await Clients.Caller.SendAsync("Error", new { message = ex.Message });
            }
        }

        // Chỉnh sửa tin nhắn
        public async Task EditMessage(string chatroomId, string messageId, string newText)
        {
            try
            {
                var userIdClaim = Context.User?.FindFirst("user_id");
                if (userIdClaim == null)
                {
                    await Clients.Caller.SendAsync("Error", new { message = "User ID not found in claims" });
                    return;
                }
                if (!Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    await Clients.Caller.SendAsync("Error", new { message = "Invalid User ID format" });
                    return;
                }

                var updatedMessage = await _messageService.EditMessageAsync(userId, Guid.Parse(messageId), newText);

                // Notify all members
                await Clients.Group(chatroomId).SendAsync("MessageEdited", updatedMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing message");
                await Clients.Caller.SendAsync("Error", new { message = ex.Message });
            }
        }

        // Reply to message
        public async Task ReplyToMessage(string chatroomId, string parentMessageId, string replyText)
        {
            try
            {
                var userIdClaim = Context.User?.FindFirst("user_id");
                if (userIdClaim == null)
                {
                    await Clients.Caller.SendAsync("Error", new { message = "User ID not found in claims" });
                    return;
                }
                if (!Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    await Clients.Caller.SendAsync("Error", new { message = "Invalid User ID format" });
                    return;
                }

                var messageDto = new SendMessageRequest
                {
                    ChatroomId = Guid.Parse(chatroomId),
                    MessageType = "text",
                    MessageText = replyText,
                    ParentMessageId = Guid.Parse(parentMessageId)
                };

                var message = await _messageService.SendMessageAsync(userId, messageDto);

                await Clients.Group(chatroomId).SendAsync("ReceiveMessage", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replying to message");
                await Clients.Caller.SendAsync("Error", new { message = ex.Message });
            }
        }

        // Gửi file
        public async Task SendFile(string chatroomId, string fileName, string fileUrl, string fileType)
        {
            try
            {
                var userIdClaim = Context.User?.FindFirst("user_id");
                if (userIdClaim == null)
                {
                    await Clients.Caller.SendAsync("Error", new { message = "User ID not found in claims" });
                    return;
                }
                if (!Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    await Clients.Caller.SendAsync("Error", new { message = "Invalid User ID format" });
                    return;
                }

                var messageDto = new SendMessageRequest
                {
                    ChatroomId = Guid.Parse(chatroomId),
                    MessageType = fileType, // "image", "file", "video", "audio"
                    MessageText = fileUrl
                };

                var message = await _messageService.SendMessageAsync(userId, messageDto);

                await Clients.Group(chatroomId).SendAsync("ReceiveMessage", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending file");
                await Clients.Caller.SendAsync("Error", new { message = ex.Message });
            }
        }

        // Gửi notification
        public async Task SendNotificationToUser(string recipientUserId, object notification)
        {
            var connections = await _connectionManager.GetConnectionsAsync(Guid.Parse(recipientUserId));

            if (connections.Any())
            {
                await Clients.Clients(connections).SendAsync("ReceiveNotification", notification);
            }
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