using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.Responses.Friends
{
    public class RespondToFriendRequest
    {
        public string Status { get; set; } = "accepted"; // accepted, rejected
    }
}