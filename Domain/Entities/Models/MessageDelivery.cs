using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;

namespace linksy_backend_api.Domain.Entities.Models
{
    public class MessageDelivery
    {
        public Guid DeliveryId { get; set; }
        public Guid MessageId { get; set; }
        public Guid UserId { get; set; }
        public string Status { get; set; } = null!;   // sent | delivered | read
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }

        // Navigation
        public virtual Message Message { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}