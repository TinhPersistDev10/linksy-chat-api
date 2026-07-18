using linksy_backend_api.DTOs.MessagesDTOs;
using linksy_backend_api.Domain.DTOs.Responses.Messages;
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
            var attachments = await unitOfWork.MessageAttachments
                .Query()
                .Where(attachment => attachment.MessageId == message.MessageId)
                .OrderBy(attachment => attachment.UploadedAt)
                .Select(attachment => new MediaAttachmentResponseDto
                {
                    AttachmentId = attachment.AttachmentId,
                    FileName = attachment.FileName ?? string.Empty,
                    FileUrl = attachment.CdnUrl ?? attachment.FilePath ?? string.Empty,
                    FileType = attachment.AttachmentType,
                    FileSize = attachment.FileSize ?? 0,
                    ThumbnailUrl = attachment.ThumbnailUrl,
                    Duration = attachment.DurationMs
                })
                .ToListAsync();

            var mentionEntities = await unitOfWork.MessageMentions
                .Query()
                .Where(m => m.MessageId == message.MessageId)
                .Include(m => m.MentionedUser)
                .ToListAsync();

            Dictionary<Guid, string> nicknameByUserId = new();
            if (mentionEntities.Count > 0)
            {
                var mentionedUserIds = mentionEntities.Select(x => x.MentionedUserId).ToList();
                var memberNicknames = await unitOfWork.ChatroomMembers
                    .Query()
                    .Where(m => m.ChatroomId == message.ChatroomId
                        && m.LeftAt == null
                        && mentionedUserIds.Contains(m.UserId)
                        && m.Nickname != null
                        && m.Nickname != "")
                    .Select(m => new { m.UserId, m.Nickname })
                    .ToListAsync();

                nicknameByUserId = memberNicknames
                    .ToDictionary(m => m.UserId, m => m.Nickname!);
            }

            var mentions = mentionEntities.Select(m =>
            {
                var user = m.MentionedUser;
                var displayName = nicknameByUserId.TryGetValue(m.MentionedUserId, out var nickname)
                    ? nickname
                    : user?.Fullname ?? user?.Username ?? string.Empty;

                return new MentionDto
                {
                    UserId = m.MentionedUserId,
                    DisplayName = displayName,
                    AvatarUrl = DefaultAvatarHelper.GetAvatarOrDefault(
                        user?.Avatar,
                        user?.UserId,
                        username: user?.Username,
                        fullname: user?.Fullname)
                };
            }).ToList();

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
                DeletedAt = message.DeletedAt,
                Attachments = attachments.Count > 0 ? attachments : null,
                Mentions = mentions.Count > 0 ? mentions : null
            };
        }
    }
}
