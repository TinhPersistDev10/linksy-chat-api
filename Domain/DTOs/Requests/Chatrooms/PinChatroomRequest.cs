using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.DTOs.ChatroomDTO
{
    public class PinChatroomRequest
    {
        [Required]
        public bool IsPinned { get; set; }
    }
}
