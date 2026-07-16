namespace linksy_backend_api.Domain.Events.Messages
{
    public class MessageDeliveredEvent
    {
        public Guid ChatroomId { get; set; }
        public Guid DeliveredBy { get; set; }
        public DateTime DeliveredAt { get; set; }
        public MessageDeliverySummaryEvent Message { get; set; } = new();
    }
}
