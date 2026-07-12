using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Domain.Interfaces.Repositories;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Services;

/// <summary>
/// Persists and authorizes the lifecycle of a one-to-one WebRTC call.
/// SignalR carries SDP/ICE only; it never owns the media stream.
/// </summary>
public sealed class CallService : ICallService
{
    private static readonly string[] ActiveStatuses = ["ringing", "answered"];

    private readonly IUnitOfWork _unitOfWork;
    private readonly IChatroomAccessService _chatroomAccessService;

    public CallService(
        IUnitOfWork unitOfWork,
        IChatroomAccessService chatroomAccessService)
    {
        _unitOfWork = unitOfWork;
        _chatroomAccessService = chatroomAccessService;
    }

    public async Task<CallLog> InitiateCallAsync(
        Guid callerId,
        Guid chatroomId,
        string callType)
    {
        callType = NormalizeCallType(callType);
        await _chatroomAccessService.EnsureMemberAsync(chatroomId, callerId);

        var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(chatroomId)
            ?? throw new KeyNotFoundException("Không tìm thấy chatroom.");

        // A single SDP offer can only be negotiated with one peer. Group calls
        // need a separate per-peer offer flow or an SFU, so keep this API safe.
        if (!string.Equals(chatroom.RoomType, "direct", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("Group call chưa được hỗ trợ ở luồng WebRTC hiện tại.");

        var memberIds = await _unitOfWork.ChatroomMemberRepository
            .GetActiveMemberIdsExceptAsync(chatroomId, callerId);

        if (memberIds.Count != 1)
            throw new InvalidOperationException("Chat 1-1 phải có đúng một người nhận cuộc gọi.");

        var now = DateTime.UtcNow;
        var call = new CallLog
        {
            Id = Guid.NewGuid(),
            CallerId = callerId,
            ChatroomId = chatroomId,
            CallType = callType,
            Status = "ringing",
            StartedAt = now,
            Participants =
            [
                new CallParticipant
                {
                    Id = Guid.NewGuid(),
                    UserId = callerId,
                    Status = "joined",
                    JoinedAt = now
                },
                new CallParticipant
                {
                    Id = Guid.NewGuid(),
                    UserId = memberIds[0],
                    Status = "invited"
                }
            ]
        };

        await _unitOfWork.CallLogs.AddAsync(call);
        // SaveChanges uses EF Core's transaction for this single write. The
        // filtered unique index on call_logs is the concurrency guarantee.
        await _unitOfWork.SaveChangesAsync();

        return call;
    }

    public async Task<CallLog> AnswerCallAsync(Guid callLogId, Guid userId)
    {
        var call = await GetCallWithParticipantsAsync(callLogId);
        var participant = GetParticipant(call, userId);

        if (call.Status != "ringing" || participant.Status != "invited")
            throw new InvalidOperationException("Cuộc gọi không còn ở trạng thái chờ trả lời.");

        var now = DateTime.UtcNow;
        participant.Status = "joined";
        participant.JoinedAt = now;
        call.Status = "answered";
        call.AnsweredAt = now;
        await _unitOfWork.SaveChangesAsync();
        return call;
    }

    public async Task<CallLog> RejectCallAsync(Guid callLogId, Guid userId)
    {
        var call = await GetCallWithParticipantsAsync(callLogId);
        var participant = GetParticipant(call, userId);

        if (call.Status != "ringing" || participant.Status != "invited")
            throw new InvalidOperationException("Cuộc gọi không còn ở trạng thái chờ từ chối.");

        var now = DateTime.UtcNow;
        participant.Status = "declined";
        participant.LeftAt = now;
        call.Status = "rejected";
        call.EndedAt = now;
        await _unitOfWork.SaveChangesAsync();
        return call;
    }

    public async Task<CallLog> EndCallAsync(Guid callLogId, Guid userId)
    {
        var call = await GetCallWithParticipantsAsync(callLogId);
        var participant = GetParticipant(call, userId);

        if (!ActiveStatuses.Contains(call.Status))
            throw new InvalidOperationException("Cuộc gọi đã kết thúc.");

        var now = DateTime.UtcNow;
        call.Status = "ended";
        call.EndedAt = now;
        call.DurationSec = call.AnsweredAt.HasValue
            ? Math.Max(0, (int)(now - call.AnsweredAt.Value).TotalSeconds)
            : 0;

        foreach (var item in call.Participants)
        {
            if (item.Status == "invited")
                item.Status = "missed";
            else if (item.Status == "joined")
                item.Status = "left";

            item.LeftAt ??= now;
        }

        // The lookup above is intentional: only a participant may terminate a call.
        _ = participant;
        await _unitOfWork.SaveChangesAsync();
        return call;
    }

    public async Task EnsureCanSignalAsync(
        Guid callLogId,
        Guid senderId,
        Guid recipientId)
    {
        if (senderId == recipientId)
            throw new ArgumentException("Không thể gửi signaling cho chính mình.");

        var call = await GetCallWithParticipantsAsync(callLogId);
        if (!ActiveStatuses.Contains(call.Status) ||
            !call.Participants.Any(participant => participant.UserId == senderId) ||
            !call.Participants.Any(participant => participant.UserId == recipientId))
        {
            throw new UnauthorizedAccessException("Không có quyền signaling cho cuộc gọi này.");
        }
    }

    private async Task<CallLog> GetCallWithParticipantsAsync(Guid callLogId) =>
        await _unitOfWork.CallLogs.Query()
            .Include(call => call.Participants)
            .SingleOrDefaultAsync(call => call.Id == callLogId)
        ?? throw new KeyNotFoundException("Không tìm thấy cuộc gọi.");

    private static CallParticipant GetParticipant(CallLog call, Guid userId) =>
        call.Participants.SingleOrDefault(participant => participant.UserId == userId)
        ?? throw new UnauthorizedAccessException("Bạn không phải là người tham gia cuộc gọi này.");

    private static string NormalizeCallType(string callType)
    {
        var normalized = callType?.Trim().ToLowerInvariant();
        return normalized is "audio" or "video"
            ? normalized
            : throw new ArgumentException("Loại cuộc gọi chỉ có thể là audio hoặc video.");
    }
}
