using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Repositories;

namespace linksy_backend_api.Domain.Interfaces.Repositories
{
    public interface IMessageDeliveryRepository : IRepository<MessageDelivery>
    {
        Task<List<MessageDelivery>> GetByMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
        Task<MessageDelivery?> GetByMessageAndUserAsync(Guid messageId, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> UpdateStatusAsync(Guid messageId, Guid userId, string status, CancellationToken cancellationToken = default);
        Task CreateDeliveriesForMembersAsync(Guid messageId, List<Guid> recipientIds, CancellationToken cancellationToken = default);
        Task MarkAsDeliveredAsync(Guid messageId, Guid userId, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(Guid messageId, Guid userId, CancellationToken cancellationToken = default);
    }
}
