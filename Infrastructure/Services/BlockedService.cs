using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.Block;
using linksy_backend_api.Models;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace linksy_backend_api.Infrastructure.Services
{
    public class BlockedService : IBlockedService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BlockedService> _logger;

        public BlockedService(IUnitOfWork unitOfWork, ILogger<BlockedService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ApiResponseDto> BlockUserAsync(Guid blockerId, BlockUserRequest request)
        {
            if (blockerId == request.BlockedUserId)
                throw new InvalidOperationException("Không thể chặn chính mình");

            var blockedUser = await _unitOfWork.Users.GetByIdAsync(request.BlockedUserId);
            if (blockedUser == null)
                throw new KeyNotFoundException("Không tìm thấy người dùng");

            var existingBlock = await _unitOfWork.BlockedUsers.Query()
                .FirstOrDefaultAsync(b => b.BlockerUserId == blockerId && b.BlockedUserId == request.BlockedUserId);

            if (existingBlock != null)
                throw new InvalidOperationException("Đã chặn người dùng này");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Remove friendship if exists
                var minId = blockerId < request.BlockedUserId ? blockerId : request.BlockedUserId;
                var maxId = blockerId > request.BlockedUserId ? blockerId : request.BlockedUserId;

                var friendship = await _unitOfWork.Friendships.Query()
                    .FirstOrDefaultAsync(f => f.User1Id == minId && f.User2Id == maxId);

                if (friendship != null)
                    _unitOfWork.Friendships.Delete(friendship);

                // Remove pending requests (both directions)
                var pendingRequests = await _unitOfWork.FriendRequests.Query()
                    .Where(fr =>
                        ((fr.SenderId == blockerId && fr.ReceiverId == request.BlockedUserId) ||
                         (fr.SenderId == request.BlockedUserId && fr.ReceiverId == blockerId)) &&
                        fr.Status == "pending")
                    .ToListAsync();

                _unitOfWork.FriendRequests.RemoveRange(pendingRequests);

                // Create block
                var block = new BlockedUser
                {
                    BlockId = Guid.NewGuid(),
                    BlockerUserId = blockerId,
                    BlockedUserId = request.BlockedUserId,
                    Reason = request.Reason,
                    BlockedAt = DateTime.UtcNow
                };

                await _unitOfWork.BlockedUsers.AddAsync(block);
                await _unitOfWork.CommitTransactionAsync();

                return new ApiResponseDto
                {
                    Success = true,
                    Message = "Đã chặn người dùng"
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<List<BlockedUserResponse>> GetBlockedUsersAsync(Guid userId)
        {
            var blocks = await _unitOfWork.BlockedUserRepository.GetBlockedUsersWithDetailsAsync(userId);

            return blocks.Select(block => new BlockedUserResponse
            {
                BlockId = block.BlockId,
                UserId = block.BlockedUserId,
                Username = block.BlockedUserNavigation?.Username ?? string.Empty,
                Fullname = block.BlockedUserNavigation?.Fullname ?? string.Empty,
                Avatar = block.BlockedUserNavigation?.Avatar ?? string.Empty,
                Reason = block.Reason ?? string.Empty,
                BlockedAt = block.BlockedAt ?? DateTime.UtcNow
            }).ToList();

        }

        public async Task<ApiResponseDto> UnblockUserAsync(Guid blockerId, Guid blockedUserId)
        {
            var block = await _unitOfWork.BlockedUserRepository.GetBlockAsync(blockerId, blockedUserId);

            if (block == null)
                throw new KeyNotFoundException("Không tìm thấy người dùng đã chặn");

            _unitOfWork.BlockedUsers.Delete(block);
            await _unitOfWork.SaveChangesAsync();

            return new ApiResponseDto
            {
                Success = true,
                Message = "Đã bỏ chặn người dùng"
            };
        }

        public async Task<(bool IBlocked, bool BlockedBy)> GetBlockStatusAsync(Guid userId, Guid otherUserId)
        {
            if (userId == otherUserId)
                return (false, false);

            var iBlocked = await _unitOfWork.BlockedUsers.Query()
                .AnyAsync(b => b.BlockerUserId == userId && b.BlockedUserId == otherUserId);

            var blockedBy = await _unitOfWork.BlockedUsers.Query()
                .AnyAsync(b => b.BlockerUserId == otherUserId && b.BlockedUserId == userId);

            return (iBlocked, blockedBy);
        }
    }
}