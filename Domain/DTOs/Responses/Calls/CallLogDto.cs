using linksy_backend_api.Domain.Entities.Models;

namespace linksy_backend_api.Domain.DTOs.Responses.Calls;

public sealed record CallLogDto(
    Guid Id,
    Guid CallerId,
    Guid ChatroomId,
    string CallType,
    string Status,
    DateTime StartedAt,
    DateTime? AnsweredAt,
    DateTime? EndedAt,
    int? DurationSec,
    IReadOnlyList<CallParticipantDto> Participants)
{
    public static CallLogDto FromEntity(CallLog call) => new(
        call.Id,
        call.CallerId,
        call.ChatroomId,
        call.CallType,
        call.Status,
        call.StartedAt,
        call.AnsweredAt,
        call.EndedAt,
        call.DurationSec,
        call.Participants
            .Select(participant => new CallParticipantDto(
                participant.UserId,
                participant.Status,
                participant.JoinedAt,
                participant.LeftAt))
            .ToList());
}
