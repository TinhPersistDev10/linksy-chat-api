namespace linksy_backend_api.Domain.DTOs.Responses.Calls;

/// <summary>
/// Structured payload embedded as JSON in Message.MessageText for messages of
/// type "call_log". Keeping this typed (instead of free-form text) lets the
/// frontend render call cards deterministically instead of parsing sentences.
/// </summary>
public sealed record CallLogMessagePayload(
    Guid CallLogId,
    string CallType,
    string Status,
    int DurationSec,
    DateTime StartedAt,
    DateTime? EndedAt,
    bool IsGroup,
    Guid CallerId,
    string? CallerName,
    string? ChatroomName);
