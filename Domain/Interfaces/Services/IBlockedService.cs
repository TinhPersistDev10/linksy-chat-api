using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.Block;

namespace linksy_backend_api.Core.Interfaces.Services
{
    public interface IBlockedService
    {
        Task<ApiResponseDto> BlockUserAsync(Guid blockerId, BlockUserRequest request);

        Task<List<BlockedUserResponse>> GetBlockedUsersAsync(Guid userId);

        Task<ApiResponseDto> UnblockUserAsync(Guid blockerId, Guid blockedUserId);

        Task<(bool IBlocked, bool BlockedBy)> GetBlockStatusAsync(Guid userId, Guid otherUserId);
    }
}