namespace linksy_backend_api.Domain.DTOs.Responses.Delivery
{
    public class MessageDeliveryStatusResponse
    {
        public Guid MessageId { get; set; }
        public int SentCount { get; set; }
        public int DeliveredCount { get; set; }
        public int ReadCount { get; set; }
        public List<MessageDeliveryResponse> Deliveries { get; set; } = new();
    }
}
