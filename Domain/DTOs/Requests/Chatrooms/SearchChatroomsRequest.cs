using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.Core.DTOs.Requests.Chatrooms
{
    public class SearchChatroomsRequest
    {
         [Required]
        [StringLength(200, MinimumLength = 1)]
        public string SearchTerm { get; set; } = string.Empty;

        public string? RoomType { get; set; } // "direct", "group"

        public bool? IncludeArchived { get; set; } = false;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;
    }
}