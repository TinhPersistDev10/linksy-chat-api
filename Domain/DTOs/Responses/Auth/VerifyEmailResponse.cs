using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.DTOs.UserDTO;

namespace linksy_backend_api.DTOs
{
    public class VerifyEmailResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public UserInfoDto User { get; set; } = new UserInfoDto();
    }
}