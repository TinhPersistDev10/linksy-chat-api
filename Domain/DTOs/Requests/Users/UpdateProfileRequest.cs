using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.Domain.DTOs.Requests.Users
{
    public class UpdateProfileRequest
    {
        [StringLength(50, MinimumLength = 3)]
        public string? Username { get; set; }

        [StringLength(100)]
        public string? Fullname { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        public DateOnly? DateOfBirth { get; set; }
    }
}
