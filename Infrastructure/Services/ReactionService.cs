using linksy_backend_api.Domain.DTOs.Requests.Reacions;
using linksy_backend_api.Domain.DTOs.Responses.Reactions;
using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.DTOs;
using linksy_backend_api.Hubs;
using linksy_backend_api.Repositories.IRepositories;
using linksy_backend_api.Services.IServices;
using Microsoft.AspNetCore.SignalR;

namespace linksy_backend_api.Infrastructure.Services
{
    public class ReactionService : IReactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IConnectionManager _connectionManager;
        private readonly ILogger<ReactionService> _logger;

        public ReactionService(
            IUnitOfWork unitOfWork,
            IHubContext<ChatHub> hubContext,
            IConnectionManager connectionManager,
            ILogger<ReactionService> logger)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
            _connectionManager = connectionManager;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // TOGGLE
        // ─────────────────────────────────────────────────────────────────────

        public async Task<ApiResponseDto> ToggleReactionAsync(
            Guid userId,
            Guid messageId,
            ToggleReactionRequest request)
        {
            // Verify message exists
            var message = await _unitOfWork.Messages.GetByIdAsync(messageId)
                ?? throw new KeyNotFoundException("Không tìm thấy tin nhắn.");

            if (message.IsDeleted == true)
                throw new InvalidOperationException("Không thể react với tin nhắn đã bị xóa.");

            // Verify caller is a member of the chatroom
            var isMember = await _unitOfWork.ChatroomMembers.AnyAsync(
                cm => cm.ChatroomId == message.ChatroomId &&
                      cm.UserId == userId &&
                      cm.LeftAt == null);

            if (!isMember)
                throw new UnauthorizedAccessException("Bạn không phải thành viên phòng chat này.");

            var existing = await _unitOfWork.MessageReactionRepository
                .GetByMessageUserEmojiAsync(messageId, userId, request.EmojiCode);

            bool added;

            if (existing is not null)
            {
                // Toggle OFF — remove existing reaction
                await _unitOfWork.MessageReactionRepository
                    .RemoveReactionAsync(messageId, userId, request.EmojiCode);
                added = false;
            }
            else
            {
                // Toggle ON — add new reaction
                var reaction = new MessageReaction
                {
                    ReactionId = Guid.NewGuid(),
                    MessageId = messageId,
                    UserId = userId,
                    EmojiCode = request.EmojiCode,
                    ReactedAt = DateTime.UtcNow
                };
                await _unitOfWork.MessageReactions.AddAsync(reaction);
                added = true;
            }

            await _unitOfWork.SaveChangesAsync();

            // Broadcast realtime update to all chatroom members
            var reactionsResponse = await GetMessageReactionsAsync(userId, messageId);
            await _hubContext.Clients
                .Group(message.ChatroomId.ToString())
                .SendAsync("ReactionUpdated", new
                {
                    MessageId = messageId,
                    ChatroomId = message.ChatroomId,
                    UserId = userId,
                    EmojiCode = request.EmojiCode,
                    Added = added,
                    Reactions = reactionsResponse
                });

            // #region agent log
            try
            {
                var line = System.Text.Json.JsonSerializer.Serialize(new
                {
                    sessionId = "50b092",
                    hypothesisId = "H3",
                    location = "ReactionService.ToggleReactionAsync",
                    message = "Reaction toggled",
                    data = new
                    {
                        messageId,
                        chatroomId = message.ChatroomId,
                        emojiCode = request.EmojiCode,
                        added,
                        totalCount = reactionsResponse.TotalCount
                    },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                await System.IO.File.AppendAllTextAsync(
                    @"d:\dotnet\chat_realtime\.cursor\debug-50b092.log",
                    line + Environment.NewLine);
            }
            catch { /* debug log only */ }
            // #endregion

            return new ApiResponseDto
            {
                Success = true,
                Message = added ? "Đã thêm reaction." : "Đã xóa reaction.",
                Data = new { Added = added, EmojiCode = request.EmojiCode }
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET
        // ─────────────────────────────────────────────────────────────────────

        public async Task<MessageReactionsResponse> GetMessageReactionsAsync(Guid userId, Guid messageId)
        {
            var reactions = await _unitOfWork.MessageReactionRepository.GetByMessageAsync(messageId);

            var grouped = reactions
                .GroupBy(r => r.EmojiCode)
                .Select(g => new ReactionSummaryResponse
                {
                    EmojiCode = g.Key,
                    Count = g.Count(),
                    ReactedByMe = g.Any(r => r.UserId == userId),
                    Users = g.Select(r => new ReactionUserResponse
                    {
                        UserId = r.UserId,
                        Username = r.User?.Username ?? string.Empty,
                        Avatar = r.User?.Avatar
                    }).ToList()
                })
                .OrderByDescending(g => g.Count)
                .ToList();

            return new MessageReactionsResponse
            {
                MessageId = messageId,
                Reactions = grouped,
                TotalCount = reactions.Count
            };
        }

       public async Task<Dictionary<Guid, MessageReactionsResponse>> GetBatchReactionsAsync(
    Guid userId,
    List<Guid> messageIds)
{
    var all = await _unitOfWork.MessageReactionRepository
        .GetByMessageIdsAsync(messageIds);

    var byMessage = all.GroupBy(r => r.MessageId);

    var result = new Dictionary<Guid, MessageReactionsResponse>();

    foreach (var id in messageIds.Distinct())
    {
        var group = byMessage.FirstOrDefault(g => g.Key == id);
        var list = group?.ToList() ?? new List<MessageReaction>();

        var summaries = list
            .GroupBy(r => r.EmojiCode)
            .Select(g => new ReactionSummaryResponse
            {
                EmojiCode = g.Key,
                Count = g.Count(),
                ReactedByMe = g.Any(r => r.UserId == userId),
                Users = g.Select(r => new ReactionUserResponse
                {
                    UserId = r.UserId,
                    Username = r.User?.Username ?? string.Empty,
                    Avatar = r.User?.Avatar
                }).ToList()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        result[id] = new MessageReactionsResponse
        {
            MessageId = id,
            Reactions = summaries,
            TotalCount = list.Count
        };
    }

            // #region agent log
            try
            {
                var line = System.Text.Json.JsonSerializer.Serialize(new
                {
                    sessionId = "50b092",
                    hypothesisId = "H4",
                    location = "ReactionService.GetBatchReactionsAsync",
                    message = "Batch reactions loaded",
                    data = new
                    {
                        requested = messageIds.Count,
                        returned = result.Count,
                        withReactions = result.Count(kv => kv.Value.TotalCount > 0)
                    },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                await System.IO.File.AppendAllTextAsync(
                    @"d:\dotnet\chat_realtime\.cursor\debug-50b092.log",
                    line + Environment.NewLine);
            }
            catch { /* debug log only */ }
            // #endregion

            return result;
        }
    }
}