namespace linksy_backend_api.Domain.Events.Messages
{
    public class MessageReadEvent
    {
        public Guid ChatroomId { get; set; }
        public Guid ReadBy { get; set; }
        public DateTime ReadAt { get; set; }
        public MessageDeliverySummaryEvent Message { get; set; } = new();
    }
}
