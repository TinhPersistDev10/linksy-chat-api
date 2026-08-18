using linksy_backend_api.Core.Interfaces.Services;

namespace linksy_backend_api.Infrastructure.Background
{
    /// <summary>
    /// Polls due scheduled messages and sends them through
    /// <see cref="IMessageService.SendMessageAsync"/> so SignalR ReceiveMessage
    /// is broadcast the same way as a normal send.
    /// </summary>
    public class ScheduledMessageDispatcher : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ScheduledMessageDispatcher> _logger;

        public ScheduledMessageDispatcher(
            IServiceScopeFactory scopeFactory,
            ILogger<ScheduledMessageDispatcher> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IScheduledMessageService>();
                    await service.DispatchDueAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Scheduled message dispatch cycle failed.");
                }

                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
