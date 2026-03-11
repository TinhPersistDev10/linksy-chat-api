using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Domain.DTOs.Responses.Chatrooms
{
    public class ChatroomStatisticsResponseDto
    {
        public int TotalMembers { get; set; }
        public int TotalMessages { get; set; }
        public int ActiveMembers { get; set; }
        public DateTime? LastActivityAt { get; set; }
    }
}