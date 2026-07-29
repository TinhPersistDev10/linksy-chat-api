using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs
{
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "Username là bắt buộc")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username phải từ 3-50 ký tự")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password là bắt buộc")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ và số")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ và số")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password là bắt buộc")]
        [Compare("Password", ErrorMessage = "Password không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [StringLength(100)]
        public string Fullname { get; set; } = string.Empty;

        public DateOnly? DateOfBirth { get; set; }
    }
}
