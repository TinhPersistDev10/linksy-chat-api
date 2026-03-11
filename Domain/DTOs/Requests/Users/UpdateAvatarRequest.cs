using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.Requests.Users
{
    public class UpdateAvatarRequest
    {
        public IFormFile? Avatar { get; set; }
    }
}