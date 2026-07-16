namespace linksy_backend_api.Domain.Events.Messages
{
    public class AllMessagesReadEvent
    {
        public Guid ChatroomId { get; set; }
        public Guid ReadBy { get; set; }
        public DateTime ReadAt { get; set; }
        public List<MessageDeliverySummaryEvent> Messages { get; set; } = new();
    }
}
