using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Repositories;

namespace linksy_backend_api.Domain.Interfaces.Repositories
{
    public interface IMessageReactionRepository : IRepository<MessageReaction>
    {
        /// <summary>Lấy tất cả reactions của một message, gom nhóm theo emoji.</summary>
        Task<List<MessageReaction>> GetByMessageAsync(Guid messageId, CancellationToken cancellationToken = default);

        Task<List<MessageReaction>> GetByMessageIdsAsync (IEnumerable<Guid> messageIds, CancellationToken cancellationToken = default);
        /// <summary>Lấy reaction của một user trên một message với emoji cụ thể.</summary>
        Task<MessageReaction?> GetByMessageUserEmojiAsync(Guid messageId, Guid userId, string emojiCode, CancellationToken cancellationToken = default);

        /// <summary>Kiểm tra user đã react với emoji này chưa.</summary>
        Task<bool> HasReactedAsync(Guid messageId, Guid userId, string emojiCode, CancellationToken cancellationToken = default);

        /// <summary>Xóa reaction.</summary>
        Task<bool> RemoveReactionAsync(Guid messageId, Guid userId, string emojiCode, CancellationToken cancellationToken = default);

        /// <summary>Đếm số reactions theo emoji trên một message.</summary>
        Task<Dictionary<string, int>> GetReactionCountsAsync(Guid messageId, CancellationToken cancellationToken = default);
        
    }
}
