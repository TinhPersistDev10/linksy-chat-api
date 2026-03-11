using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.Responses.Users
{
    public class AvatarResponse
    {
         public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
}