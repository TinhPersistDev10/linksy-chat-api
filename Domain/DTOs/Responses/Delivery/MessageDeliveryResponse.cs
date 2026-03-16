namespace linksy_backend_api.Domain.DTOs.Responses.Delivery
{
    public class MessageDeliveryResponse
    {
        public Guid DeliveryId { get; set; }
        public Guid MessageId { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        /// <summary>sent | delivered | read</summary>
        public string Status { get; set; } = string.Empty;
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
