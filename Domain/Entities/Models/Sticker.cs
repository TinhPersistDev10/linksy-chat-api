using linksy_backend_api.Models;

namespace linksy_backend_api.Domain.Entities.Models
{
    public class Sticker
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public string ImageUrl { get; set; } = null!;
        public string PublicId { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public virtual User Owner { get; set; } = null!;
    }
}
