using linksy_backend_api.DTOs.MessagesDTOs;

namespace linksy_backend_api.Core.Interfaces.Services;

public interface IScheduledMessageService
{
    Task<ScheduledMessageResponse> ScheduleAsync(Guid userId, ScheduleMessageRequest request);
    Task<List<ScheduledMessageResponse>> GetPendingMineAsync(Guid userId, Guid chatroomId);
    Task CancelAsync(Guid userId, Guid scheduledMessageId);
    Task DispatchDueAsync(CancellationToken cancellationToken);
}
