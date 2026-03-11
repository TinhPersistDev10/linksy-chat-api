using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs.Auth
{
    public class RegisterResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; } = false;
    }
}