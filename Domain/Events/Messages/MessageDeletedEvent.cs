namespace linksy_backend_api.Domain.Events.Messages
{
    public class MessageDeletedEvent
    {
        public Guid MessageId { get; set; }
        public Guid ChatroomId { get; set; }
        public Guid DeletedBy { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}