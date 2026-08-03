using System.ComponentModel.DataAnnotations;

namespace linksy_backend_api.Domain.DTOs.Requests.Users
{
    public class UpdateProfileRequest
    {
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username phải từ 3-50 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username chỉ được chứa chữ, số và dấu gạch dưới")]
        public string? Username { get; set; }

        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ và tên phải từ 2-100 ký tự")]
        public string? Fullname { get; set; }

        [StringLength(500, ErrorMessage = "Giới thiệu không được quá 500 ký tự")]
        public string? Bio { get; set; }

        public DateOnly? DateOfBirth { get; set; }
    }
}

