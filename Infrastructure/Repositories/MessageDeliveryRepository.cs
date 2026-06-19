using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Domain.Interfaces.Repositories;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Repositories
{
    public class MessageDeliveryRepository : Repository<MessageDelivery>, IMessageDeliveryRepository
    {
        private readonly LinksyDbContext _context;

        public MessageDeliveryRepository(LinksyDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<MessageDelivery>> GetByMessageAsync(
            Guid messageId,
            CancellationToken cancellationToken = default)
        {
            return await _context.MessageDeliveries
                .Include(d => d.User)
                .Where(d => d.MessageId == messageId)
                .ToListAsync(cancellationToken);
        }

        public async Task<MessageDelivery?> GetByMessageAndUserAsync(
            Guid messageId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.MessageDeliveries
                .FirstOrDefaultAsync(d => d.MessageId == messageId && d.UserId == userId, cancellationToken);
        }

        public async Task<bool> UpdateStatusAsync(
            Guid messageId,
            Guid userId,
            string status,
            CancellationToken cancellationToken = default)
        {
            var delivery = await GetByMessageAndUserAsync(messageId, userId, cancellationToken);
            if (delivery is null) return false;

            delivery.Status = status;
            if (status == "delivered") delivery.DeliveredAt = DateTime.UtcNow;
            if (status == "read") delivery.ReadAt = DateTime.UtcNow;

            _context.MessageDeliveries.Update(delivery);
            return true;
        }

        public async Task CreateDeliveriesForMembersAsync(
            Guid messageId,
            List<Guid> recipientIds,
            CancellationToken cancellationToken = default)
        {
            var deliveries = recipientIds.Select(userId => new MessageDelivery
            {
                DeliveryId = Guid.NewGuid(),
                MessageId = messageId,
                UserId = userId,
                Status = "sent"
            }).ToList();

            await _context.MessageDeliveries.AddRangeAsync(deliveries, cancellationToken);
        }

        public async Task MarkAsDeliveredAsync(
            Guid messageId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var delivery = await GetByMessageAndUserAsync(messageId, userId, cancellationToken);
            if (delivery is null) return;

            if (delivery.Status == "sent")
            {
                delivery.Status = "delivered";
                delivery.DeliveredAt = DateTime.UtcNow;
                _context.MessageDeliveries.Update(delivery);
            }
        }

        public async Task MarkAsReadAsync(
            Guid messageId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var delivery = await GetByMessageAndUserAsync(messageId, userId, cancellationToken);
            if (delivery is null) return;

            delivery.Status = "read";
            delivery.ReadAt = DateTime.UtcNow;
            if (!delivery.DeliveredAt.HasValue) delivery.DeliveredAt = DateTime.UtcNow;
            _context.MessageDeliveries.Update(delivery);
        }

        public async Task<List<MessageDelivery>> GetByMessageIdsAsync(IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken = default)
        {
            return await _context.MessageDeliveries
                .Where(d => messageIds.Contains(d.MessageId))
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Guid>> MarkAllAsReadAsync(Guid chatroomId, Guid userId, DateTime readAt, CancellationToken cancellationToken = default)
        {
            var deliveries = await _context.MessageDeliveries
                .Where(d =>
                    d.UserId == userId &&
                    d.Message.ChatroomId == chatroomId &&
                    d.Status != "read")
                .ToListAsync(cancellationToken);

            foreach (var delivery in deliveries)
            {
                delivery.Status = "read";
                delivery.DeliveredAt ??= readAt;
                delivery.ReadAt ??= readAt;
            }

            return deliveries.Select(d => d.MessageId).ToList();
        }
    }
}
