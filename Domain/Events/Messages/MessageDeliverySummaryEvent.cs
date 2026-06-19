namespace linksy_backend_api.Domain.Events.Messages
{
    public class MessageDeliverySummaryEvent
    {
        public Guid MessageId { get; set; }
        public int RecipientCount { get; set; }
        public int DeliveredCount { get; set; }
        public int ReadCount { get; set; }
        public string DeliveryStatus { get; set; } = "sent";
    }
}
