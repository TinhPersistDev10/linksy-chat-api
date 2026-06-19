using linksy_backend_api.DTOs.MessagesDTOs;
using linksy_backend_api.Infrastructure.Helpers;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Mappers
{
    public static class MessageMapper
    {
        public static async Task<MessageResponse> ToResponseAsync(
            Message message,
            IUnitOfWork unitOfWork,
            Guid? currentUserId = null)
        {
            var sender = message.Sender ?? await unitOfWork.Users.GetByIdAsync(message.SenderId ?? Guid.Empty);

            // Parent message
            MessageResponse? parentMessageDto = null;
            if (message.ParentMessageId.HasValue)
            {
                var parent = await unitOfWork.Messages.Query()
                    .Include(m => m.Sender)
                    .FirstOrDefaultAsync(m => m.MessageId == message.ParentMessageId.Value);

                if (parent != null)
                {
                    var parentIsDeleted = parent.IsDeleted ?? false;

                    parentMessageDto = new MessageResponse
                    {
                        MessageId = parent.MessageId,
                        ChatroomId = parent.ChatroomId,

                        SenderId = parent.SenderId ?? Guid.Empty,
                        SenderUsername = parent.Sender?.Username ?? string.Empty,
                        SenderFullname = parent.Sender?.Fullname ?? string.Empty,
                        SenderAvatar = DefaultAvatarHelper.GetAvatarOrDefault(
                            parent.Sender?.Avatar,
                            parent.Sender?.UserId,
                            username: parent.Sender?.Username,
                            fullname: parent.Sender?.Fullname
                        ),

                        MessageType = parent.MessageType,
                        MessageText = parentIsDeleted
                            ? "Tin nhắn đã bị xóa"
                            : parent.MessageText ?? string.Empty,

                        ParentMessageId = parent.ParentMessageId,
                        ParentMessage = null,

                        IsEdited = parent.IsEdited ?? false,
                        IsDeleted = parentIsDeleted,
                        IsOwn = currentUserId.HasValue &&
                                parent.SenderId == currentUserId.Value,

                        SentAt = parent.SentAt ?? DateTime.UtcNow,
                        EditedAt = parent.EditedAt,
                        DeletedAt = parent.DeletedAt,
                        Attachments = null
                    };
                }
            }

            return new MessageResponse
            {
                MessageId = message.MessageId,
                ChatroomId = message.ChatroomId,
                SenderId = message.SenderId ?? Guid.Empty,
                SenderUsername = sender?.Username ?? string.Empty,
                SenderFullname = sender?.Fullname ?? string.Empty,
                SenderAvatar = DefaultAvatarHelper.GetAvatarOrDefault(
                                    sender?.Avatar, sender?.UserId,
                                    username: sender?.Username,
                                    fullname: sender?.Fullname),
                MessageType = message.MessageType,
                MessageText = message.MessageText ?? string.Empty,
                ParentMessageId = message.ParentMessageId,
                ParentMessage = parentMessageDto,
                IsEdited = message.IsEdited ?? false,
                IsDeleted = message.IsDeleted ?? false,
                IsOwn = currentUserId.HasValue && message.SenderId == currentUserId,
                SentAt = message.SentAt ?? DateTime.UtcNow,
                EditedAt = message.EditedAt,
                DeletedAt = message.DeletedAt
            };
        }
    }
}