using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.Domain.DTOs.Requests.Reacions
{
    public class ToggleReactionRequest
    {
        [Required(ErrorMessage = "EmojiCode là bắt buộc")]
        [StringLength(10, ErrorMessage = "EmojiCode tối đa 10 ký tự")]
        public string EmojiCode { get; set; } = string.Empty;
    }
}
