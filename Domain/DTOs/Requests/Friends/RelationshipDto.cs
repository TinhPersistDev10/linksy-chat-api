using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs.RelationshipDTO
{
    public class RelationshipDto
    {
        public Guid UserId { get; set; }
        public string Status { get; set; } = string.Empty; // "none", "friends", "request_sent", "request_received", "blocked", "blocked_by"
        public Guid? RequestId { get; set; }
    }
}