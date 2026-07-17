using linksy_backend_api.DTOs.MessagesDTOs;

namespace linksy_backend_api.Domain.Events.Messages
{
    public class MessagePinnedEvent
    {
        public Guid ChatroomId { get; set; }
        public PinnedMessageResponse PinnedMessage { get; set; } = null!;
    }
}
