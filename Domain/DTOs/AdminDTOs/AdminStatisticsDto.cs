using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.AdminDTOs
{
    public class AdminStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int TotalMessages { get; set; }
        public int TotalChatrooms { get; set; }
        public int NewUsersThisMonth { get; set; }
    }
}