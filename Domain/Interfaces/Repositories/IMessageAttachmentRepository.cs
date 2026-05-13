using linksy_backend_api.Domain.Entities.Models;
using System.Threading.Tasks;

namespace linksy_backend_api.Domain.Interfaces.Repositories
{
    public interface IMessageAttachmentRepository
    {
        Task<List<MessageAttachment>> GetByMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
        Task<List<MessageAttachment>> GetByChatroomAsync(Guid chatroomId, string? attachmentType = null, int limit = 50, CancellationToken cancellationToken = default);
        Task<bool> DeleteByMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
    }
}
