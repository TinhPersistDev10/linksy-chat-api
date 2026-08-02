using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.DTOs.MessagesDTOs
{
    public class CreatePollRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Question { get; set; } = string.Empty;

        [Required]
        [MinLength(2)]
        [MaxLength(10)]
        public List<string> Options { get; set; } = new();
    }
}
