using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Domain.Interfaces.Repositories;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Services;

/// <summary>
/// Persists and authorizes WebRTC call lifecycle. SignalR carries SDP/ICE only;
/// this service never owns media streams.
/// </summary>
public sealed class CallService : ICallService
{
    private static readonly string[] ActiveStatuses = ["ringing", "answered"];
    private static readonly string[] SignalableParticipantStatuses = ["invited", "joined"];//

    private readonly IUnitOfWork _unitOfWork;
    private readonly IChatroomAccessService _chatroomAccessService;

    public CallService(IUnitOfWork unitOfWork, IChatroomAccessService chatroomAccessService)
    {
        _unitOfWork = unitOfWork;
        _chatroomAccessService = chatroomAccessService;
    }

    public async Task<CallLog> InitiateCallAsync(Guid callerId, Guid chatroomId, string callType)
    {
        callType = NormalizeCallType(callType);
        await _chatroomAccessService.EnsureMemberAsync(chatroomId, callerId);

        var memberIds = await _unitOfWork.ChatroomMemberRepository
            .GetActiveMemberIdsExceptAsync(chatroomId, callerId);

        if (memberIds.Count == 0)
            throw new InvalidOperationException("The chatroom must have at least one call recipient.");

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
                }
            ]
        };

        foreach (var memberId in memberIds)
        {
            call.Participants.Add(new CallParticipant
            {
                Id = Guid.NewGuid(),
                UserId = memberId,
                Status = "invited"
            });
        }

        await _unitOfWork.CallLogs.AddAsync(call);
        await _unitOfWork.SaveChangesAsync();

        return call;
    }

    public async Task<CallLog> AnswerCallAsync(Guid callLogId, Guid userId)
    {
        return await JoinCallAsync(callLogId, userId);
    }

    public async Task<CallLog> JoinCallAsync(Guid callLogId, Guid userId)
    {
        var call = await GetCallWithParticipantsAsync(callLogId);
        var participant = GetParticipant(call, userId);

        if (!ActiveStatuses.Contains(call.Status))
            throw new InvalidOperationException("The call is no longer active.");

        if (participant.Status == "joined")
            return call;

        if (participant.Status != "invited")
            throw new InvalidOperationException("This participant cannot join the call.");

        var now = DateTime.UtcNow;
        participant.Status = "joined";
        participant.JoinedAt ??= now;
        participant.LeftAt = null;
        call.Status = "answered";
        call.AnsweredAt ??= now;

        await _unitOfWork.SaveChangesAsync();
        return await GetCallWithParticipantsAsync(callLogId);
    }

    public async Task<CallLog> RejectCallAsync(Guid callLogId, Guid userId)
    {
        var call = await GetCallWithParticipantsAsync(callLogId);
        var participant = GetParticipant(call, userId);

        if (!ActiveStatuses.Contains(call.Status) || participant.Status != "invited")
            throw new InvalidOperationException("The call is no longer waiting for this participant.");

        var now = DateTime.UtcNow;
        participant.Status = "declined";
        participant.LeftAt = now;

        var invitedParticipants = call.Participants
            .Where(item => item.UserId != call.CallerId)
            .ToList();
        var hasJoinedInvitees = invitedParticipants.Any(item => item.Status == "joined");
        var allInviteesDeclined = invitedParticipants.Count > 0 &&
            invitedParticipants.All(item => item.Status == "declined");

        if (!hasJoinedInvitees && allInviteesDeclined)
        {
            call.Status = "rejected";
            call.EndedAt = now;
            call.DurationSec = 0;
        }

        await _unitOfWork.SaveChangesAsync();
        return await GetCallWithParticipantsAsync(callLogId);
    }

    public async Task<CallLog> EndCallAsync(Guid callLogId, Guid userId)
    {
        var call = await GetCallWithParticipantsAsync(callLogId);
        var participant = GetParticipant(call, userId);

        if (!ActiveStatuses.Contains(call.Status))
            throw new InvalidOperationException("The call has already ended.");

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

        _ = participant;
        await _unitOfWork.SaveChangesAsync();
        return await GetCallWithParticipantsAsync(callLogId);
    }

    public async Task<CallLog> LeaveCallAsync(Guid callLogId, Guid userId)
    {
        var call = await GetCallWithParticipantsAsync(callLogId);
        var participant = GetParticipant(call, userId);

        if (!ActiveStatuses.Contains(call.Status))
            return call;

        var now = DateTime.UtcNow;
        if (participant.Status == "joined")
        {
            participant.Status = "left";
            participant.LeftAt ??= now;
        }
        else if (participant.Status == "invited")
        {
            participant.Status = "declined";
            participant.LeftAt ??= now;
        }

        var hasJoinedParticipants = call.Participants.Any(item =>
            item.UserId != userId && item.Status == "joined");

        if (!hasJoinedParticipants)
        {
            call.Status = "ended";
            call.EndedAt = now;
            call.DurationSec = call.AnsweredAt.HasValue
                ? Math.Max(0, (int)(now - call.AnsweredAt.Value).TotalSeconds)
                : 0;

            foreach (var item in call.Participants)
            {
                if (item.Status == "invited")
                    item.Status = "missed";

                item.LeftAt ??= now;
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return await GetCallWithParticipantsAsync(callLogId);
    }

    public async Task<CallLog> GetCallForParticipantAsync(Guid callLogId, Guid userId)
    {
        var call = await GetCallWithParticipantsAsync(callLogId);
        _ = GetParticipant(call, userId);
        return call;
    }

    public async Task<CallLog?> GetActiveCallForChatroomAsync(Guid chatroomId, Guid userId)
    {
        await _chatroomAccessService.EnsureMemberAsync(chatroomId, userId);

        return await _unitOfWork.CallLogs.Query()
            .Include(call => call.Participants)
            .Where(call =>
                call.ChatroomId == chatroomId &&
                ActiveStatuses.Contains(call.Status) &&
                call.Participants.Any(participant => participant.UserId == userId))
            .OrderByDescending(call => call.StartedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<CallLog>> HandleParticipantDisconnectedAsync(Guid userId)
    {
        var activeCalls = await _unitOfWork.CallLogs.Query()
            .Include(call => call.Chatroom)
            .Include(call => call.Participants)
            .Where(call =>
                ActiveStatuses.Contains(call.Status) &&
                call.Participants.Any(participant =>
                    participant.UserId == userId &&
                    SignalableParticipantStatuses.Contains(participant.Status)))
            .ToListAsync();

        var changedCalls = new List<CallLog>();
        foreach (var call in activeCalls)
        {
            var changedCall = string.Equals(
                    call.Chatroom?.RoomType,
                    "direct",
                    StringComparison.OrdinalIgnoreCase)
                ? await EndCallAsync(call.Id, userId)
                : await LeaveCallAsync(call.Id, userId);

            changedCalls.Add(changedCall);
        }

        return changedCalls;
    }

    public async Task EnsureCanSignalAsync(Guid callLogId, Guid senderId, Guid recipientId)
    {
        if (senderId == recipientId)
            throw new ArgumentException("Cannot signal yourself.");

        var call = await GetCallWithParticipantsAsync(callLogId);
        var sender = GetParticipant(call, senderId);
        var recipient = GetParticipant(call, recipientId);

        if (!ActiveStatuses.Contains(call.Status) ||
            !SignalableParticipantStatuses.Contains(sender.Status) ||
            !SignalableParticipantStatuses.Contains(recipient.Status))
        {
            throw new UnauthorizedAccessException("Not allowed to signal for this call.");
        }
    }

    private async Task<CallLog> GetCallWithParticipantsAsync(Guid callLogId) =>
        await _unitOfWork.CallLogs.Query()
            .Include(call => call.Participants)
            .SingleOrDefaultAsync(call => call.Id == callLogId)
        ?? throw new KeyNotFoundException("Call not found.");

    private static CallParticipant GetParticipant(CallLog call, Guid userId) =>
        call.Participants.SingleOrDefault(participant => participant.UserId == userId)
        ?? throw new UnauthorizedAccessException("You are not a participant in this call.");

    private static string NormalizeCallType(string callType)
    {
        var normalized = callType?.Trim().ToLowerInvariant();
        return normalized is "audio" or "video"
            ? normalized
            : throw new ArgumentException("Call type must be audio or video.");
    }
}
