using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.DTOs.MessagesDTOs
{
    public class VotePollRequest
    {
        [Required]
        public Guid OptionId { get; set; }
    }
}
