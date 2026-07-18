using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.Domain.DTOs.Requests.Reacions
{
    public class ToggleReactionRequest
    
    {
        [Required(ErrorMessage = "EmojiCode là bắt buộc")]
        [StringLength(64, MinimumLength = 1, ErrorMessage = "EmojiCode từ 1 đến 64 ký tự")]
        public string EmojiCode { get; set; } = string.Empty;
    }
}
