using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Email hoặc Username là bắt buộc")]
        public string EmailOrUsername { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password là bắt buộc")]
        public string Password { get; set; } = string.Empty;
    }
}