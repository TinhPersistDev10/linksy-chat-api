using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.DTOs.Block;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace linksy_backend_api.API.Controllers
{
    [Authorize]
    [EnableRateLimiting("api")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class BlockedUserController : ControllerBase
    {
        private readonly IBlockedService _blockedService;
        private readonly ILogger<BlockedUserController> _logger;

        public BlockedUserController(IBlockedService blockedService, ILogger<BlockedUserController> logger)
        {
            _blockedService = blockedService;
            _logger = logger;
        }

        [HttpPost("block")]
        public async Task<IActionResult> BlockUser([FromBody] BlockUserRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }
                var result = await _blockedService.BlockUserAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking user");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// Unblock user
        [HttpDelete("block/{blockedUserId}")]
        public async Task<IActionResult> UnblockUser(Guid blockedUserId)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }
                var result = await _blockedService.UnblockUserAsync(userId, blockedUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unblocking user");
                return BadRequest(new { message = ex.Message });
            }
        }
        /// Lấy danh sách users đã block
        [HttpGet("blocked")]
        public async Task<IActionResult> GetBlockedUsers()
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }
                var blockedUsers = await _blockedService.GetBlockedUsersAsync(userId);
                return Ok(blockedUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blocked users");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// Kiểm tra trạng thái chặn 2 chiều với user khác
        [HttpGet("status/{otherUserId:guid}")]
        public async Task<IActionResult> GetBlockStatus(Guid otherUserId)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                var (iBlocked, blockedBy) = await _blockedService.GetBlockStatusAsync(userId, otherUserId);
                return Ok(new { iBlocked, blockedBy });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting block status with {OtherUserId}", otherUserId);
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}