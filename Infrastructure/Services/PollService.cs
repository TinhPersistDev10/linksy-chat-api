using linksy_backend_api.Domain.DTOs.Responses.Messages;
using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.DTOs.MessagesDTOs;
using linksy_backend_api.Hubs;
using linksy_backend_api.Infrastructure.Cache;
using linksy_backend_api.Infrastructure.Mappers;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Services
{
    public class PollService : IPollService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ICacheService _cache;
        private readonly ILogger<PollService> _logger;

        public PollService(
            IUnitOfWork unitOfWork,
            IHubContext<ChatHub> hubContext,
            ICacheService cache,
            ILogger<PollService> logger)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
            _cache = cache;
            _logger = logger;
        }

        public async Task<PollResponse> VoteAsync(Guid userId, Guid messageId, VotePollRequest request)
        {
            var message = await _unitOfWork.Messages.GetByIdAsync(messageId)
                ?? throw new KeyNotFoundException("Không tìm thấy tin nhắn.");

            if (message.IsDeleted == true)
                throw new InvalidOperationException("Không thể bình chọn tin nhắn đã xóa.");

            if (!string.Equals(message.MessageType, "poll", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Tin nhắn này không phải bình chọn.");

            var isMember = await _unitOfWork.ChatroomMembers.AnyAsync(cm =>
                cm.ChatroomId == message.ChatroomId &&
                cm.UserId == userId &&
                cm.LeftAt == null);
            if (!isMember)
                throw new UnauthorizedAccessException("Bạn không phải thành viên phòng chat này.");

            var poll = await _unitOfWork.MessagePolls.Query()
                .Include(p => p.Options)
                .FirstOrDefaultAsync(p => p.MessageId == messageId)
                ?? throw new KeyNotFoundException("Không tìm thấy bình chọn.");

            if (poll.IsClosed)
                throw new InvalidOperationException("Bình chọn đã đóng.");

            var option = poll.Options.FirstOrDefault(o => o.OptionId == request.OptionId)
                ?? throw new ArgumentException("Lựa chọn không hợp lệ.");

            var existing = await _unitOfWork.MessagePollVotes.Query()
                .FirstOrDefaultAsync(v => v.MessageId == messageId && v.UserId == userId);

            if (existing is not null && existing.OptionId == option.OptionId)
            {
                return await BuildPollResponseAsync(messageId, userId, message.ChatroomId)
                    ?? throw new KeyNotFoundException("Không tìm thấy bình chọn.");
            }

            if (existing is null)
            {
                await _unitOfWork.MessagePollVotes.AddAsync(new MessagePollVote
                {
                    VoteId = Guid.NewGuid(),
                    OptionId = option.OptionId,
                    MessageId = messageId,
                    UserId = userId,
                    VotedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.OptionId = option.OptionId;
                existing.VotedAt = DateTime.UtcNow;
                _unitOfWork.MessagePollVotes.Update(existing);
            }

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException) when (existing is null)
            {
                // Concurrent first vote — treat as update to the chosen option.
                var raced = await _unitOfWork.MessagePollVotes.Query()
                    .FirstOrDefaultAsync(v => v.MessageId == messageId && v.UserId == userId)
                    ?? throw new InvalidOperationException("Không thể lưu bình chọn.");

                raced.OptionId = option.OptionId;
                raced.VotedAt = DateTime.UtcNow;
                _unitOfWork.MessagePollVotes.Update(raced);
                await _unitOfWork.SaveChangesAsync();
            }

            var response = await BuildPollResponseAsync(messageId, userId, message.ChatroomId)
                ?? throw new KeyNotFoundException("Không tìm thấy bình chọn.");

            await BroadcastPollUpdatedAsync(
                message.ChatroomId,
                messageId,
                response,
                actorUserId: userId,
                actorOptionId: option.OptionId);
            return response;
        }

        public async Task<PollResponse> CloseAsync(Guid userId, Guid messageId)
        {
            var message = await _unitOfWork.Messages.GetByIdAsync(messageId)
                ?? throw new KeyNotFoundException("Không tìm thấy tin nhắn.");

            if (message.IsDeleted == true)
                throw new InvalidOperationException("Không thể hủy bình chọn đã xóa.");

            if (!string.Equals(message.MessageType, "poll", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Tin nhắn này không phải bình chọn.");

            var poll = await _unitOfWork.MessagePolls.GetByIdAsync(messageId)
                ?? throw new KeyNotFoundException("Không tìm thấy bình chọn.");

            if (poll.IsClosed)
                throw new InvalidOperationException("Bình chọn đã hủy.");

            var canClose = await CanClosePollAsync(userId, message.ChatroomId, poll.CreatedByUserId);
            if (!canClose)
                throw new UnauthorizedAccessException("Bạn không có quyền hủy bình chọn này.");

            poll.IsClosed = true;
            poll.ClosedAt = DateTime.UtcNow;
            _unitOfWork.MessagePolls.Update(poll);

            var actor = await _unitOfWork.Users.GetByIdAsync(userId);
            var actorName = actor?.Fullname ?? actor?.Username ?? "Ai đó";
            var cancelNotice = new Message
            {
                MessageId = Guid.NewGuid(),
                ChatroomId = message.ChatroomId,
                SenderId = userId,
                MessageType = "system",
                MessageText = $"{actorName} đã hủy bình chọn",
                SentAt = DateTime.UtcNow,
                IsEdited = false,
                IsDeleted = false
            };
            await _unitOfWork.Messages.AddAsync(cancelNotice);

            var recipientIds = await _unitOfWork.ChatroomMemberRepository
                .GetActiveMemberIdsExceptAsync(message.ChatroomId, userId);
            await _unitOfWork.MessageDeliveryRepository.CreateDeliveriesForMembersAsync(
                cancelNotice.MessageId,
                recipientIds);

            var chatroom = await _unitOfWork.Chatrooms.GetByIdAsync(message.ChatroomId);
            if (chatroom is not null)
            {
                chatroom.LastMessageId = cancelNotice.MessageId;
                chatroom.LastMessageAt = cancelNotice.SentAt;
                chatroom.LastActivityAt = cancelNotice.SentAt;
                _unitOfWork.Chatrooms.Update(chatroom);
            }

            await _unitOfWork.SaveChangesAsync();

            var response = await BuildPollResponseAsync(messageId, userId, message.ChatroomId)
                ?? throw new KeyNotFoundException("Không tìm thấy bình chọn.");

            await BroadcastPollUpdatedAsync(
                message.ChatroomId,
                messageId,
                response,
                actorUserId: null,
                actorOptionId: null);

            try
            {
                var noticeResponse = await MessageMapper.ToResponseAsync(
                    cancelNotice, _unitOfWork, userId);
                await _hubContext.Clients
                    .Group(message.ChatroomId.ToString())
                    .SendAsync("ReceiveMessage", noticeResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to broadcast poll cancel notice. MessageId={MessageId}",
                    messageId);
            }

            await InvalidateChatroomListCacheAsync(recipientIds.Append(userId));
            return response;
        }

        public Task<PollResponse?> GetPollAsync(Guid messageId, Guid userId)
            => BuildPollResponseAsync(messageId, userId, chatroomId: null);

        public async Task<Dictionary<Guid, PollResponse>> GetPollsByMessageIdsAsync(
            IEnumerable<Guid> messageIds,
            Guid userId)
        {
            var ids = messageIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<Guid, PollResponse>();

            var polls = await _unitOfWork.MessagePolls.Query()
                .Where(p => ids.Contains(p.MessageId))
                .Include(p => p.Options)
                .ThenInclude(o => o.Votes)
                .ToListAsync();

            if (polls.Count == 0)
                return new Dictionary<Guid, PollResponse>();

            var messageChatroomMap = await _unitOfWork.Messages.Query()
                .Where(m => ids.Contains(m.MessageId))
                .Select(m => new { m.MessageId, m.ChatroomId })
                .ToListAsync();
            var chatroomByMessage = messageChatroomMap.ToDictionary(x => x.MessageId, x => x.ChatroomId);

            var chatroomIds = chatroomByMessage.Values.Distinct().ToList();
            var adminMemberships = await _unitOfWork.ChatroomMembers.Query()
                .Where(cm =>
                    chatroomIds.Contains(cm.ChatroomId) &&
                    cm.UserId == userId &&
                    cm.LeftAt == null &&
                    cm.MemberRole == "admin")
                .Select(cm => cm.ChatroomId)
                .ToListAsync();
            var adminSet = adminMemberships.ToHashSet();

            var result = new Dictionary<Guid, PollResponse>();
            foreach (var poll in polls)
            {
                chatroomByMessage.TryGetValue(poll.MessageId, out var chatroomId);
                var isAdmin = adminSet.Contains(chatroomId);
                result[poll.MessageId] = MapPoll(poll, userId, isAdmin);
            }

            return result;
        }

        private async Task<PollResponse?> BuildPollResponseAsync(
            Guid messageId,
            Guid userId,
            Guid? chatroomId)
        {
            var poll = await _unitOfWork.MessagePolls.Query()
                .Where(p => p.MessageId == messageId)
                .Include(p => p.Options)
                .ThenInclude(o => o.Votes)
                .FirstOrDefaultAsync();

            if (poll is null)
                return null;

            Guid roomId = chatroomId ?? Guid.Empty;
            if (roomId == Guid.Empty)
            {
                var message = await _unitOfWork.Messages.GetByIdAsync(messageId);
                roomId = message?.ChatroomId ?? Guid.Empty;
            }

            var isAdmin = false;
            if (roomId != Guid.Empty)
            {
                isAdmin = await _unitOfWork.ChatroomMembers.AnyAsync(cm =>
                    cm.ChatroomId == roomId &&
                    cm.UserId == userId &&
                    cm.LeftAt == null &&
                    cm.MemberRole == "admin");
            }

            return MapPoll(poll, userId, isAdmin);
        }

        private static PollResponse MapPoll(MessagePoll poll, Guid userId, bool isAdmin)
        {
            var options = poll.Options
                .OrderBy(o => o.SortOrder)
                .Select(o =>
                {
                    var voteCount = o.Votes?.Count ?? 0;
                    var votedByMe = o.Votes?.Any(v => v.UserId == userId) == true;
                    return new PollOptionResponse
                    {
                        OptionId = o.OptionId,
                        Text = o.Text,
                        SortOrder = o.SortOrder,
                        VoteCount = voteCount,
                        VotedByMe = votedByMe
                    };
                })
                .ToList();

            var myVote = options.FirstOrDefault(o => o.VotedByMe);

            return new PollResponse
            {
                MessageId = poll.MessageId,
                Question = poll.Question,
                IsClosed = poll.IsClosed,
                CreatedByUserId = poll.CreatedByUserId,
                TotalVotes = options.Sum(o => o.VoteCount),
                MyVotedOptionId = myVote?.OptionId,
                CanClose = !poll.IsClosed && (poll.CreatedByUserId == userId || isAdmin),
                Options = options
            };
        }

        private async Task<bool> CanClosePollAsync(Guid userId, Guid chatroomId, Guid createdByUserId)
        {
            var member = await _unitOfWork.ChatroomMembers.Query()
                .FirstOrDefaultAsync(cm =>
                    cm.ChatroomId == chatroomId &&
                    cm.UserId == userId &&
                    cm.LeftAt == null);

            if (member is null)
                return false;

            return userId == createdByUserId
                || string.Equals(member.MemberRole, "admin", StringComparison.OrdinalIgnoreCase);
        }

        private Task InvalidateChatroomListCacheAsync(IEnumerable<Guid> userIds)
        {
            var tasks = userIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .SelectMany(userId => new[]
                {
                    _cache.RemoveAsync(CacheKeys.ChatroomList(userId, false, null)),
                    _cache.RemoveAsync(CacheKeys.ChatroomList(userId, true, null)),
                    _cache.RemoveAsync(CacheKeys.ChatroomList(userId, false, "direct")),
                    _cache.RemoveAsync(CacheKeys.ChatroomList(userId, false, "group")),
                    _cache.RemoveAsync(CacheKeys.ChatroomList(userId, true, "direct")),
                    _cache.RemoveAsync(CacheKeys.ChatroomList(userId, true, "group")),
                });
            return Task.WhenAll(tasks);
        }

        private async Task BroadcastPollUpdatedAsync(
            Guid chatroomId,
            Guid messageId,
            PollResponse poll,
            Guid? actorUserId,
            Guid? actorOptionId)
        {
            try
            {
                // Strip per-user flags so each client merges against its own state.
                var shared = new PollResponse
                {
                    MessageId = poll.MessageId,
                    Question = poll.Question,
                    IsClosed = poll.IsClosed,
                    CreatedByUserId = poll.CreatedByUserId,
                    TotalVotes = poll.TotalVotes,
                    MyVotedOptionId = null,
                    CanClose = false,
                    Options = poll.Options.Select(o => new PollOptionResponse
                    {
                        OptionId = o.OptionId,
                        Text = o.Text,
                        SortOrder = o.SortOrder,
                        VoteCount = o.VoteCount,
                        VotedByMe = false
                    }).ToList()
                };

                await _hubContext.Clients
                    .Group(chatroomId.ToString())
                    .SendAsync("PollUpdated", new PollUpdatedEvent
                    {
                        MessageId = messageId,
                        ChatroomId = chatroomId,
                        ActorUserId = actorUserId,
                        ActorOptionId = actorOptionId,
                        Poll = shared
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SignalR broadcast failed for PollUpdated. MessageId={MessageId}",
                    messageId);
            }
        }
    }
}
