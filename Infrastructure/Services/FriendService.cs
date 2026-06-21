using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.DTOs.Requests.Friends;
using linksy_backend_api.Core.DTOs.Responses.Friends;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Domain.DTOs.Responses.Users;
using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.Block;
using linksy_backend_api.DTOs.FriendDTO;
using linksy_backend_api.DTOs.RelationshipDTO;
using linksy_backend_api.DTOs.UserDTO;
using linksy_backend_api.Infrastructure.Helpers;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using linksy_backend_api.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Services
{
    public class FriendService : IFriendService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConnectionManager _connectionManager;
        private readonly ILogger<FriendService> _logger;
        private readonly INotificationService _notificationService;
        public FriendService(
            IUnitOfWork unitOfWork,
            IConnectionManager connectionManager,
            ILogger<FriendService> logger,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _connectionManager = connectionManager;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<ApiResponseDto> AcceptFriendRequestAsync(Guid userId, Guid requestId)
        {
            var request = await _unitOfWork.FriendRequests.GetByIdAsync(requestId);
            if (request == null)
            {
                throw new KeyNotFoundException("Không tìm thấy yêu cầu kết bạn");
            }

            // Kiểm tra người nhận có phải là user hiện tại không
            if (request.ReceiverId != userId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền chấp nhận yêu cầu này");
            }

            // Kiểm tra status
            if (request.Status != "pending")
            {
                throw new InvalidOperationException("Yêu cầu kết bạn đã được xử lý");
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Cập nhật status của request
                request.Status = "accepted";
                request.RespondedAt = DateTime.UtcNow;
                _unitOfWork.FriendRequests.Update(request);

                // Tạo friendship mới - đảm bảo User1Id < User2Id để tránh duplicate
                var minId = request.SenderId < request.ReceiverId ? request.SenderId : request.ReceiverId;
                var maxId = request.SenderId > request.ReceiverId ? request.SenderId : request.ReceiverId;

                var friendship = new Friendship
                {
                    FriendshipId = Guid.NewGuid(),
                    User1Id = minId,
                    User2Id = maxId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Friendships.AddAsync(friendship);

                // Tạo notification cho sender
                await _notificationService.NotifyFriendRequestAcceptedAsync(request.ReceiverId, request.SenderId, friendship.FriendshipId);

                await _unitOfWork.CommitTransactionAsync();

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "Đã chấp nhận yêu cầu kết bạn"
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ApiResponseDto> CancelFriendRequestAsync(Guid userId, Guid requestId)
        {
            var request = await _unitOfWork.FriendRequests.GetByIdAsync(requestId);
            if (request == null)
            {
                throw new KeyNotFoundException("Không tìm thấy yêu cầu kết bạn");
            }

            if (request.SenderId != userId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền hủy yêu cầu này");
            }

            if (request.Status != "pending")
            {
                throw new InvalidOperationException("Yêu cầu kết bạn đã được xử lý");
            }

            _unitOfWork.FriendRequests.Delete(request);
            await _unitOfWork.SaveChangesAsync();

            return new ApiResponseDto
            {
                Success = true,
                Message = "Đã hủy yêu cầu kết bạn"
            };
        }

        public async Task<List<FriendDto>> GetFriendsAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("Getting friends for user {UserId}", userId);
                var friendships = await _unitOfWork.Friendships.Query()
                .Where(f => f.User1Id == userId || f.User2Id == userId)
                .ToListAsync();

                var friends = new List<FriendDto>();

                foreach (var friendship in friendships)
                {
                    var friendId = friendship.User1Id == userId ? friendship.User2Id : friendship.User1Id;
                    var friend = await _unitOfWork.Users.GetByIdAsync(friendId);

                    if (friend != null)
                    {
                        friends.Add(new FriendDto
                        {
                            UserId = friend.UserId,
                            Username = friend.Username,
                            Fullname = friend.Fullname,
                            Avatar = DefaultAvatarHelper.GetAvatarOrDefault(friend.Avatar, friend.UserId),
                            FriendsSince = friendship.CreatedAt ?? DateTime.UtcNow
                        });
                    }
                }
                _logger.LogInformation("Found {FriendCount} friends for user {UserId}", friends.Count, userId);
                return friends;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error getting friends for user {UserId}", userId);
                throw;
            }

        }
        //Lấy danh sách yêu cầu kết bạn nhận được
        public async Task<List<FriendRequestResponse>> GetReceivedFriendRequestsAsync(Guid userId)
        {
            var requests = await _unitOfWork.FriendRequestRepository.GetPendingRequestsAsync(userId);

            var result = new List<FriendRequestResponse>();
            foreach (var request in requests)
            {
                var sender = await _unitOfWork.Users.GetByIdAsync(request.SenderId);
                var receiver = await _unitOfWork.Users.GetByIdAsync(request.ReceiverId);

                result.Add(new FriendRequestResponse
                {
                    RequestId = request.RequestId,
                    SenderId = request.SenderId,
                    SenderUsername = sender?.Username,
                    SenderFullname = sender?.Fullname,
                    SenderAvatar = DefaultAvatarHelper.GetAvatarOrDefault(sender?.Avatar, sender?.UserId),
                    ReceiverId = request.ReceiverId,
                    ReceiverUsername = receiver?.Username,
                    ReceiverFullname = receiver?.Fullname,
                    ReceiverAvatar = DefaultAvatarHelper.GetAvatarOrDefault(receiver?.Avatar, receiver?.UserId),
                    Status = request.Status,
                    Message = request.Message,
                    SentAt = request.SentAt,
                    RespondedAt = request.RespondedAt
                });
            }
            return result;
        }

        public async Task<RelationshipDto> GetRelationshipAsync(Guid userId, Guid otherUserId)
        {
            if (userId == otherUserId)
            {
                return new RelationshipDto { Status = "self" };
            }

            // Check if blocked
            var isBlocked = await _unitOfWork.BlockedUsers.Query()
                .FirstOrDefaultAsync(b => b.BlockerUserId == userId && b.BlockedUserId == otherUserId);

            if (isBlocked != null)
            {
                return new RelationshipDto { Status = "blocked" };
            }

            var isBlockedBy = await _unitOfWork.BlockedUsers.Query()
                .FirstOrDefaultAsync(b => b.BlockerUserId == otherUserId && b.BlockedUserId == userId);

            if (isBlockedBy != null)
            {
                return new RelationshipDto { Status = "blocked_by" };
            }

            // Check if friends
            var minId = userId < otherUserId ? userId : otherUserId;
            var maxId = userId > otherUserId ? userId : otherUserId;

            var areFriends = await _unitOfWork.Friendships.Query()
                .AnyAsync(f => f.User1Id == minId && f.User2Id == maxId);

            if (areFriends)
            {
                return new RelationshipDto { Status = "friends" };
            }

            // Check friend requests
            var sentRequest = await _unitOfWork.FriendRequests.Query()
                .FirstOrDefaultAsync(fr => fr.SenderId == userId &&
                                          fr.ReceiverId == otherUserId &&
                                          fr.Status == "pending");

            if (sentRequest != null)
            {
                return new RelationshipDto
                {
                    Status = "request_sent",
                    RequestId = sentRequest.RequestId
                };
            }

            var receivedRequest = await _unitOfWork.FriendRequests.Query()
                .FirstOrDefaultAsync(fr => fr.SenderId == otherUserId &&
                                          fr.ReceiverId == userId &&
                                          fr.Status == "pending");

            if (receivedRequest != null)
            {
                return new RelationshipDto
                {
                    Status = "request_received",
                    RequestId = receivedRequest.RequestId
                };
            }

            return new RelationshipDto { Status = "none" };
        }
        public async Task<List<FriendRequestResponse>> GetSentFriendRequestsAsync(Guid userId)
        {
            var requests = await _unitOfWork.FriendRequests.Query()
        .Where(fr => fr.SenderId == userId && fr.Status == "pending")
        .OrderByDescending(fr => fr.SentAt)
        .ToListAsync();

            var result = new List<FriendRequestResponse>();
            foreach (var request in requests)
            {
                var sender = await _unitOfWork.Users.GetByIdAsync(request.SenderId);
                var receiver = await _unitOfWork.Users.GetByIdAsync(request.ReceiverId);

                result.Add(new FriendRequestResponse
                {
                    RequestId = request.RequestId,
                    SenderId = request.SenderId,
                    SenderUsername = sender?.Username,
                    SenderFullname = sender?.Fullname,
                    SenderAvatar = DefaultAvatarHelper.GetAvatarOrDefault(sender?.Avatar, sender?.UserId),
                    ReceiverId = request.ReceiverId,
                    ReceiverUsername = receiver?.Username,
                    ReceiverFullname = receiver?.Fullname,
                    ReceiverAvatar = DefaultAvatarHelper.GetAvatarOrDefault(receiver?.Avatar, receiver?.UserId),
                    Status = request.Status,
                    Message = request.Message,
                    SentAt = request.SentAt,
                    RespondedAt = request.RespondedAt
                });
            }
            return result;
        }

        public async Task<ApiResponseDto> RejectFriendRequestAsync(Guid userId, Guid requestId)
        {
            var request = await _unitOfWork.FriendRequests.GetByIdAsync(requestId);
            if (request == null)
            {
                throw new KeyNotFoundException("Không tìm thấy yêu cầu kết bạn");
            }

            if (request.ReceiverId != userId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền từ chối yêu cầu này");
            }

            if (request.Status != "pending")
            {
                throw new InvalidOperationException("Yêu cầu kết bạn đã được xử lý");
            }

            request.Status = "rejected";
            request.RespondedAt = DateTime.UtcNow;
            _unitOfWork.FriendRequests.Update(request);
            await _unitOfWork.SaveChangesAsync();

            return new ApiResponseDto
            {
                Success = true,
                Message = "Đã từ chối yêu cầu kết bạn"
            };
        }

        public async Task<List<UserSearchDto>> SearchUsersAsync(Guid userId, string query, int limit)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return new List<UserSearchDto>();
                }

                _logger.LogInformation("Searching users with query: {Query}", query);

                var lowerQuery = query.ToLower();
                var users = await _unitOfWork.Users.Query()
                    .Where(u => u.UserId != userId &&
                        (u.Username.ToLower().Contains(lowerQuery) ||
                        (u.Fullname != null && u.Fullname.ToLower().Contains(lowerQuery)) ||
                        (u.Email != null && u.Email.ToLower() == lowerQuery))) // exact match cho email
                    .Take(limit)
                    .ToListAsync();

                var results = new List<UserSearchDto>();

                foreach (var user in users)
                {
                    var relationship = await GetRelationshipAsync(userId, user.UserId);

                    results.Add(new UserSearchDto
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        Fullname = user.Fullname,
                        Avatar = DefaultAvatarHelper.GetAvatarOrDefault(user.Avatar, user.UserId),
                        Bio = user.Bio,
                        RelationshipStatus = relationship.Status,          // raw key, không phải text
                        ActionButtonText = GetActionButtonText(relationship.Status),
                        CanSendRequest = relationship.Status == "none"
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users");
                throw;
            }
        }

        private string GetRelationshipStatusText(string status)
        {
            return status switch
            {
                "friends" => "Bạn bè",
                "request_sent" => "Đã gửi lời mời",
                "request_received" => "Đã nhận lời mời",
                "blocked" => "Đã chặn",
                "blocked_by" => "Đã bị chặn",
                "self" => "Chính bạn",
                _ => ""
            };
        }

        private string GetActionButtonText(string status)
        {
            return status switch
            {
                "friends" => "Bạn bè",
                "request_sent" => "Đã gửi",
                "request_received" => "Chấp nhận",
                "blocked" => "Đã chặn",
                "blocked_by" => "Không khả dụng",
                "self" => "",
                _ => "Kết bạn"
            };
        }

        public async Task<FriendRequestResponse> SendFriendRequestAsync(Guid senderId, SendFriendRequest request)
        {
            // Validate: Không thể gửi request cho chính mình
            if (senderId == request.ReceiverId)
            {
                throw new InvalidOperationException("Không thể gửi lời mời kết bạn cho chính mình");
            }

            // Kiểm tra receiver có tồn tại không
            var receiver = await _unitOfWork.Users.GetByIdAsync(request.ReceiverId);
            if (receiver == null)
            {
                throw new KeyNotFoundException("Không tìm thấy người dùng");
            }

            // Kiểm tra đã bị block chưa
            var isBlocked = await _unitOfWork.BlockedUsers.AnyAsync(b =>
                (b.BlockerUserId == senderId && b.BlockedUserId == request.ReceiverId) ||
                (b.BlockerUserId == request.ReceiverId && b.BlockedUserId == senderId));

            if (isBlocked)
            {
                throw new InvalidOperationException("Không thể gửi lời mời kết bạn cho người dùng này");
            }

            // Kiểm tra đã là bạn bè chưa
            var areFriends = await _unitOfWork.FriendshipRepository.AreFriendsAsync(senderId, request.ReceiverId);
            if (areFriends)
            {
                throw new InvalidOperationException("Đã là bạn bè");
            }

            // Kiểm tra đã gửi request chưa
            var existingRequest = await _unitOfWork.FriendRequestRepository
                    .GetRequestAsync(senderId, request.ReceiverId);

            // Kiểm tra receiver đã gửi request cho sender chưa
            var reverseRequest = await _unitOfWork.FriendRequestRepository
    .GetRequestAsync(request.ReceiverId, senderId);

            if (reverseRequest != null && reverseRequest.Status == "pending")
            {
                throw new InvalidOperationException("Người dùng này đã gửi lời mời kết bạn cho bạn");
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                FriendRequest friendRequest;

                // 7. ĐÃ TỒN TẠI REQUEST CŨ → UPDATE
                if (existingRequest != null)
                {
                    if (existingRequest.Status == "pending")
                        throw new InvalidOperationException("Đã gửi lời mời kết bạn");

                    existingRequest.Status = "pending";
                    existingRequest.Message = request.Message;
                    existingRequest.SentAt = DateTime.UtcNow;
                    existingRequest.RespondedAt = null;

                    _unitOfWork.FriendRequests.Update(existingRequest);

                    friendRequest = existingRequest;
                }
                else
                {
                    // 8. CHƯA TỒN TẠI → INSERT MỚI
                    friendRequest = new FriendRequest
                    {
                        RequestId = Guid.NewGuid(),
                        SenderId = senderId,
                        ReceiverId = request.ReceiverId,
                        Message = request.Message,
                        Status = "pending",
                        SentAt = DateTime.UtcNow
                    };

                    await _unitOfWork.FriendRequests.AddAsync(friendRequest);
                }

                await _unitOfWork.SaveChangesAsync();

                // 9. Tạo notification
                await _notificationService.NotifyNewFriendRequestAsync(
                    senderId,
                    request.ReceiverId,
                    friendRequest.RequestId);

                await _unitOfWork.CommitTransactionAsync();

                // 10. Trả response
                var sender = await _unitOfWork.Users.GetByIdAsync(senderId);

                return new FriendRequestResponse
                {
                    RequestId = friendRequest.RequestId,
                    SenderId = senderId,
                    SenderUsername = sender!.Username,
                    SenderFullname = sender.Fullname,
                    SenderAvatar = sender.Avatar,

                    ReceiverId = receiver.UserId,
                    ReceiverUsername = receiver.Username,
                    ReceiverFullname = receiver.Fullname,
                    ReceiverAvatar = receiver.Avatar,

                    Status = friendRequest.Status,
                    Message = friendRequest.Message,
                    SentAt = friendRequest.SentAt
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error sending friend request");
                throw;
            }
        }


        public async Task<ApiResponseDto> UnfriendAsync(Guid userId, Guid friendId)
        {
            var minId = userId < friendId ? userId : friendId;
            var maxId = userId > friendId ? userId : friendId;

            var friendship = await _unitOfWork.Friendships.Query()
                .FirstOrDefaultAsync(f => f.User1Id == minId && f.User2Id == maxId);

            if (friendship == null)
            {
                throw new KeyNotFoundException("Không tìm thấy mối quan hệ bạn bè");
            }

            _unitOfWork.Friendships.Delete(friendship);
            await _unitOfWork.SaveChangesAsync();

            return new ApiResponseDto
            {
                Success = true,
                Message = "Đã hủy kết bạn"
            };
        }
    }
}