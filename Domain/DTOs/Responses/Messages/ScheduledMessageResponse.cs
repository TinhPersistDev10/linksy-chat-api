namespace linksy_backend_api.DTOs.MessagesDTOs;

public class ScheduledMessageResponse
{
    public Guid Id { get; set; }
    public Guid ChatroomId { get; set; }
    public Guid SenderId { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public Guid? ParentMessageId { get; set; }
    public DateTime SendAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
