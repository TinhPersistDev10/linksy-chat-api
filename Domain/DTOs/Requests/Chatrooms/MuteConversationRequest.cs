using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.DTOs.ChatroomDTO
{
    public class MuteConversationRequest
    {
        [Required]
        public bool IsMuted { get; set; }

        public DateTime? MuteUntil { get; set; }
    }
}
