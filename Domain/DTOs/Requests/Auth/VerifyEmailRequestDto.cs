using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs
{
    public class VerifyEmailRequestDto
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "OTP là bắt buộc")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP phải có 6 ký tự")]
        public string OtpCode { get; set; } = string.Empty;
    }
}