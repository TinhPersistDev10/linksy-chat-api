namespace linksy_backend_api.Domain.DTOs.Responses.Stickers
{
    public class StickerResponse
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
