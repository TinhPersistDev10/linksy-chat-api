namespace linksy_backend_api.DTOs.MessagesDTOs
{
    public class PinnedMessageResponse
    {
        public Guid PinnedMessageId { get; set; }
        public Guid ChatroomId { get; set; }
        public Guid MessageId { get; set; }
        public string MessageType { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public Guid SenderId { get; set; }
        public string SenderFullname { get; set; } = string.Empty;
        public Guid PinnedByUserId { get; set; }
        public string PinnedByName { get; set; } = string.Empty;
        public DateTime PinnedAt { get; set; }
    }
}
