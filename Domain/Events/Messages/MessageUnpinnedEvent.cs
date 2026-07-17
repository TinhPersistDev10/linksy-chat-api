namespace linksy_backend_api.Domain.Events.Messages
{
    public class MessageUnpinnedEvent
    {
        public Guid ChatroomId { get; set; }
        public Guid MessageId { get; set; }
        public Guid UnpinnedBy { get; set; }
    }
}
