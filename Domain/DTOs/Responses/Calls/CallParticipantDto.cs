namespace linksy_backend_api.Domain.DTOs.Responses.Calls;

public sealed record CallParticipantDto(
    Guid UserId,
    string Status,
    DateTime? JoinedAt,
    DateTime? LeftAt);
