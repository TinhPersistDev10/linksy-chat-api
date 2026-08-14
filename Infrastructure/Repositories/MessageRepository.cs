using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Repositories
{
    public class MessageRepository : Repository<Message>, IMessageRepository
    {
        private readonly LinksyDbContext _context;
        public MessageRepository(LinksyDbContext context) : base(context)
        {
            _context = context;
        }

        private static IQueryable<Message> Visible(IQueryable<Message> query, Guid chatroomId, DateTime? after)
        {
            query = query.Where(m =>
                m.ChatroomId == chatroomId &&
                (m.IsDeleted == null || m.IsDeleted == false));

            if (after.HasValue)
                query = query.Where(m => m.SentAt > after.Value);

            return query;
        }

        public async Task<List<Message>> GetChatroomMessagesAsync(
            Guid chatroomId, int page, int pageSize, DateTime? after = null)
        {
            return await Visible(Query(), chatroomId, after)
                .Include(m => m.Sender)
                .Include(m => m.MessageAttachments)
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<(List<Message> Messages, bool HasMoreBefore)> GetMessagesAroundAsync(
            Guid chatroomId,
            Guid messageId,
            int beforeCount,
            int afterCount,
            DateTime? after = null)
        {
            var targetQuery = Visible(Query(), chatroomId, after)
                .Include(m => m.Sender)
                .Include(m => m.MessageAttachments);

            var target = await targetQuery.FirstOrDefaultAsync(m => m.MessageId == messageId);

            if (target is null)
                return (new List<Message>(), false);

            var older = await Visible(Query(), chatroomId, after)
                .Include(m => m.Sender)
                .Include(m => m.MessageAttachments)
                .Where(m =>
                    m.SentAt < target.SentAt ||
                    (m.SentAt == target.SentAt && m.MessageId.CompareTo(target.MessageId) < 0))
                .OrderByDescending(m => m.SentAt)
                .ThenByDescending(m => m.MessageId)
                .Take(beforeCount + 1)
                .ToListAsync();

            var hasMoreBefore = older.Count > beforeCount;
            if (hasMoreBefore)
                older = older.Take(beforeCount).ToList();

            var newer = await Visible(Query(), chatroomId, after)
                .Include(m => m.Sender)
                .Include(m => m.MessageAttachments)
                .Where(m =>
                    m.SentAt > target.SentAt ||
                    (m.SentAt == target.SentAt && m.MessageId.CompareTo(target.MessageId) > 0))
                .OrderBy(m => m.SentAt)
                .ThenBy(m => m.MessageId)
                .Take(afterCount)
                .ToListAsync();

            older.Reverse();
            var result = new List<Message>(older.Count + 1 + newer.Count);
            result.AddRange(older);
            result.Add(target);
            result.AddRange(newer);
            return (result, hasMoreBefore);
        }

        public async Task<(List<Message> Messages, bool HasMore)> GetMessagesBeforeAsync(
            Guid chatroomId,
            Guid beforeMessageId,
            int pageSize,
            DateTime? after = null)
        {
            var anchor = await Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(m =>
                    m.MessageId == beforeMessageId &&
                    m.ChatroomId == chatroomId);

            if (anchor is null)
                return (new List<Message>(), false);

            // Anchor itself may be before clearedAt — still page older messages within the visible window.
            var batch = await Visible(Query(), chatroomId, after)
                .Include(m => m.Sender)
                .Include(m => m.MessageAttachments)
                .Where(m =>
                    m.SentAt < anchor.SentAt ||
                    (m.SentAt == anchor.SentAt && m.MessageId.CompareTo(anchor.MessageId) < 0))
                .OrderByDescending(m => m.SentAt)
                .ThenByDescending(m => m.MessageId)
                .Take(pageSize + 1)
                .ToListAsync();

            var hasMore = batch.Count > pageSize;
            if (hasMore)
                batch = batch.Take(pageSize).ToList();

            batch.Reverse();
            return (batch, hasMore);
        }

        public async Task<Message?> GetLastMessageAsync(Guid chatroomId)
        {
            return await Query()
                .Where(m => m.ChatroomId == chatroomId)
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync();
        }

        public async Task<Message?> GetLastMessageAfterAsync(Guid chatroomId, DateTime after)
        {
            return await Visible(Query(), chatroomId, after)
                .Include(m => m.Sender)
                .Include(m => m.MessageAttachments)
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Message>> GetRepliesAsync(Guid parentMessageId)
        {
            return await Query()
                .Include(m => m.Sender)
                .Include(m => m.MessageAttachments)
                .Where(m => m.ParentMessageId == parentMessageId
                    && (m.IsDeleted == null
                    || m.IsDeleted == false))
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid chatroomId, Guid userId, DateTime lastReadAt)
        {
            return await CountAsync(m =>
                m.ChatroomId == chatroomId &&
                m.SenderId != userId &&
                m.SentAt > lastReadAt &&
                (m.IsDeleted == null || m.IsDeleted == false));
        }

        public async Task<Message?> GetWithSenderAsync(Guid messageId)
        {
            return await Query()
                .Include(m => m.Sender)
                .Include(m => m.MessageAttachments)
                .FirstOrDefaultAsync(m => m.MessageId == messageId);
        }

        public async Task<List<Message>> SearchMessageAsync(
            Guid chatroomId, string keyword, int limit = 50, DateTime? after = null)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return new List<Message>();
            var normalized = keyword.Trim().ToLower();

            return await Visible(Query(), chatroomId, after)
                        .Include(m => m.Sender)
                        .Include(m => m.MessageAttachments)
                        .Where(m =>
                        m.MessageText != null
                        && m.MessageText.ToLower().Contains(normalized))
                        .OrderByDescending(m => m.SentAt)
                        .Take(limit)
                        .ToListAsync();
        }
    }
}
