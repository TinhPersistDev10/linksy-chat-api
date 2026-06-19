namespace linksy_backend_api.Domain.Events.Messages
{
    public class MessageEditedEvent
    {
        public Guid MessageId { get; set; }
        public Guid ChatroomId { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
        public Guid EditedBy { get; set; }
    }
}