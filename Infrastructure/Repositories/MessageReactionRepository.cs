using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Domain.Interfaces.Repositories;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Repositories
{
    public class MessageReactionRepository : Repository<MessageReaction>, IMessageReactionRepository
    {
        private readonly LinksyDbContext _context;

        public MessageReactionRepository(LinksyDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<MessageReaction>> GetByMessageAsync(
            Guid messageId,
            CancellationToken cancellationToken = default)
        {
            return await _context.MessageReactions
                .Include(r => r.User)
                .Where(r => r.MessageId == messageId)
                .OrderBy(r => r.ReactedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<MessageReaction?> GetByMessageUserEmojiAsync(
            Guid messageId,
            Guid userId,
            string emojiCode,
            CancellationToken cancellationToken = default)
        {
            return await _context.MessageReactions
                .FirstOrDefaultAsync(r =>
                    r.MessageId == messageId &&
                    r.UserId == userId &&
                    r.EmojiCode == emojiCode,
                    cancellationToken);
        }

        public async Task<bool> HasReactedAsync(
            Guid messageId,
            Guid userId,
            string emojiCode,
            CancellationToken cancellationToken = default)
        {
            return await _context.MessageReactions
                .AnyAsync(r =>
                    r.MessageId == messageId &&
                    r.UserId == userId &&
                    r.EmojiCode == emojiCode,
                    cancellationToken);
        }

        public async Task<bool> RemoveReactionAsync(
            Guid messageId,
            Guid userId,
            string emojiCode,
            CancellationToken cancellationToken = default)
        {
            var reaction = await GetByMessageUserEmojiAsync(messageId, userId, emojiCode, cancellationToken);
            if (reaction is null) return false;

            _context.MessageReactions.Remove(reaction);
            return true;
        }

        public async Task<Dictionary<string, int>> GetReactionCountsAsync(
            Guid messageId,
            CancellationToken cancellationToken = default)
        {
            return await _context.MessageReactions
                .Where(r => r.MessageId == messageId)
                .GroupBy(r => r.EmojiCode)
                .Select(g => new { Emoji = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Emoji, x => x.Count, cancellationToken);
        }
        public async Task<List<MessageReaction>> GetByMessageIdsAsync(
            IEnumerable<Guid> messageIds,
            CancellationToken cancellationToken = default)
        {
            var ids = messageIds.Distinct().ToList();
            if(ids.Count == 0) return new List<MessageReaction>();
            return await _context.MessageReactions
                .Include(r => r.User)
                .Where(r => ids.Contains(r.MessageId))
                .OrderBy(r => r.ReactedAt)
                .ToListAsync(cancellationToken);
        }
    }
}