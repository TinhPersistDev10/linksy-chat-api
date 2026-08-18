using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Domain.Enums;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.DTOs.MessagesDTOs;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Services;

public class ScheduledMessageService : IScheduledMessageService
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text",
        "sticker"
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly IChatroomAccessService _chatroomAccess;
    private readonly IContentModerationService _contentModeration;
    private readonly ILogger<ScheduledMessageService> _logger;

    public ScheduledMessageService(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        IChatroomAccessService chatroomAccess,
        IContentModerationService contentModeration,
        ILogger<ScheduledMessageService> logger)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _chatroomAccess = chatroomAccess;
        _contentModeration = contentModeration;
        _logger = logger;
    }

    public async Task<ScheduledMessageResponse> ScheduleAsync(Guid userId, ScheduleMessageRequest request)
    {
        var messageType = (request.MessageType ?? string.Empty).Trim().ToLowerInvariant();
        var messageText = request.MessageText?.Trim() ?? string.Empty;

        if (!AllowedTypes.Contains(messageType))
            throw new ArgumentException("Chỉ có thể hẹn giờ tin nhắn chữ hoặc sticker.");
        if (string.IsNullOrWhiteSpace(messageText))
            throw new ArgumentException("Nội dung tin nhắn không được để trống.");
        if (messageText.Length > 5000)
            throw new ArgumentException("Tin nhắn không được vượt quá 5000 ký tự.");

        var sendAt = NormalizeUtc(request.SendAt);
        if (sendAt <= DateTime.UtcNow.AddSeconds(15))
            throw new ArgumentException("Thời gian gửi phải ở tương lai.");

        var isMember = await _unitOfWork.ChatroomMembers.AnyAsync(rm =>
            rm.ChatroomId == request.ChatroomId &&
            rm.UserId == userId &&
            rm.LeftAt == null);
        if (!isMember)
            throw new UnauthorizedAccessException("Bạn không phải là thành viên của phòng chat này.");

        await _chatroomAccess.EnsurePermissionAsync(
            request.ChatroomId,
            userId,
            PermissionType.CanSendMessages);

        _contentModeration.EnsureAllowed(messageText);

        if (request.ParentMessageId.HasValue)
        {
            var parent = await _unitOfWork.Messages.GetByIdAsync(request.ParentMessageId.Value)
                ?? throw new KeyNotFoundException("Không tìm thấy tin nhắn gốc.");
            if (parent.ChatroomId != request.ChatroomId)
                throw new ArgumentException("Tin nhắn gốc không thuộc phòng chat này.");
            if (parent.IsDeleted == true)
                throw new InvalidOperationException("Không thể trả lời tin nhắn đã bị xóa.");
        }

        var entity = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            ChatroomId = request.ChatroomId,
            SenderId = userId,
            MessageType = messageType,
            MessageText = messageText,
            ParentMessageId = request.ParentMessageId,
            SendAt = sendAt,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ScheduledMessages.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return ToResponse(entity);
    }

    public async Task<List<ScheduledMessageResponse>> GetPendingMineAsync(Guid userId, Guid chatroomId)
    {
        var isMember = await _unitOfWork.ChatroomMembers.AnyAsync(rm =>
            rm.ChatroomId == chatroomId &&
            rm.UserId == userId &&
            rm.LeftAt == null);
        if (!isMember)
            throw new UnauthorizedAccessException("Bạn không phải là thành viên của phòng chat này.");

        var items = await _unitOfWork.ScheduledMessages.Query()
            .Where(s =>
                s.ChatroomId == chatroomId &&
                s.SenderId == userId &&
                s.Status == "pending")
            .OrderBy(s => s.SendAt)
            .ToListAsync();

        return items.Select(ToResponse).ToList();
    }

    public async Task CancelAsync(Guid userId, Guid scheduledMessageId)
    {
        var item = await _unitOfWork.ScheduledMessages.GetByIdAsync(scheduledMessageId)
            ?? throw new KeyNotFoundException("Không tìm thấy tin nhắn hẹn giờ.");

        if (item.SenderId != userId)
            throw new UnauthorizedAccessException("Bạn chỉ có thể hủy tin nhắn hẹn giờ của mình.");
        if (!string.Equals(item.Status, "pending", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Tin nhắn hẹn giờ này không còn chờ gửi.");

        item.Status = "cancelled";
        _unitOfWork.ScheduledMessages.Update(item);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DispatchDueAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var due = await _unitOfWork.ScheduledMessages.Query()
            .Where(s => s.Status == "pending" && s.SendAt <= now)
            .OrderBy(s => s.SendAt)
            .Take(25)
            .ToListAsync(cancellationToken);

        foreach (var item in due)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.Equals(item.Status, "pending", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                await _messageService.SendMessageAsync(item.SenderId, new SendMessageRequest
                {
                    ChatroomId = item.ChatroomId,
                    MessageType = item.MessageType,
                    MessageText = item.MessageText,
                    ParentMessageId = item.ParentMessageId
                });

                item.Status = "sent";
                _unitOfWork.ScheduledMessages.Update(item);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException
                or KeyNotFoundException
                or ArgumentException
                or InvalidOperationException)
            {
                _logger.LogWarning(
                    ex,
                    "Scheduled message {Id} cancelled after send failed permanently",
                    item.Id);
                item.Status = "cancelled";
                _unitOfWork.ScheduledMessages.Update(item);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled message {Id} send failed; will retry", item.Id);
            }
        }
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static ScheduledMessageResponse ToResponse(ScheduledMessage entity) => new()
    {
        Id = entity.Id,
        ChatroomId = entity.ChatroomId,
        SenderId = entity.SenderId,
        MessageType = entity.MessageType,
        MessageText = entity.MessageText,
        ParentMessageId = entity.ParentMessageId,
        SendAt = entity.SendAt,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt
    };
}
