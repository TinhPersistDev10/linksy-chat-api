using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Domain.Interfaces.Repositories;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Repositories
{
    public class MessageAttachmentRepository : Repository<MessageAttachment>, IMessageAttachmentRepository
    {
        private readonly LinksyDbContext _context;

        public MessageAttachmentRepository(LinksyDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<MessageAttachment>> GetByMessageAsync(
            Guid messageId,
            CancellationToken cancellationToken = default)
        {
            return await _context.MessageAttachments
                .Where(a => a.MessageId == messageId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<MessageAttachment>> GetByChatroomAsync(
            Guid chatroomId,
            string? attachmentType = null,
            int limit = 50,
            CancellationToken cancellationToken = default)
        {
            var query = _context.MessageAttachments
                .Include(a => a.Message)
                .Where(a => a.Message.ChatroomId == chatroomId &&
                            (a.Message.IsDeleted == null || a.Message.IsDeleted == false));

            if (!string.IsNullOrWhiteSpace(attachmentType))
                query = query.Where(a => a.AttachmentType == attachmentType);

            return await query
                .OrderByDescending(a => a.UploadedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> DeleteByMessageAsync(
            Guid messageId,
            CancellationToken cancellationToken = default)
        {
            var attachments = await GetByMessageAsync(messageId, cancellationToken);
            if (!attachments.Any()) return false;

            _context.MessageAttachments.RemoveRange(attachments);
            return true;
        }
    }
}