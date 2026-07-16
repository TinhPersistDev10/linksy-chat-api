using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using linksy_backend_api.DTOs.Block;
using linksy_backend_api.DTOs.FriendDTO;
using linksy_backend_api.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace linksy_backend_api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class FriendsController : ControllerBase
    {
        private readonly IFriendService _friendService;
        private readonly ILogger<FriendsController> _logger;

        public FriendsController(IFriendService friendService, ILogger<FriendsController> logger)
        {
            _friendService = friendService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách bạn bè
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFriends()
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                var friends = await _friendService.GetFriendsAsync(userId);
                return Ok(friends);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting friends");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Tìm kiếm users
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string query, [FromQuery] int page = 20)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }
                var users = await _friendService.SearchUsersAsync(userId, query, page);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Gửi lời mời kết bạn
        /// </summary>
        [HttpPost("requests")]
        public async Task<IActionResult> SendFriendRequest([FromBody] SendFriendRequest request)
        {
            try
            {
                // Debug: Log tất cả claims để xem có gì
                var allClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
                _logger.LogInformation("User claims: {@Claims}", allClaims);

                // Lấy ID người gửi từ token - thử các claim type phổ biến
                var senderIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                 ?? User.FindFirst("uid")?.Value
                                 ?? User.FindFirst("user_id")?.Value
                                 ?? User.FindFirst("sub")?.Value
                                 ?? User.FindFirst("client_id")?.Value;

                _logger.LogInformation("Found senderIdClaim: {SenderIdClaim}", senderIdClaim);

                if (string.IsNullOrEmpty(senderIdClaim))
                {
                    return Unauthorized(new
                    {
                        message = "Invalid user ID",
                        claims = allClaims
                    });
                }

                // Parse GUID - xử lý cả GUID có format khác nhau
                Guid senderId;
                if (!Guid.TryParse(senderIdClaim, out senderId))
                {
                    // Thử parse nếu có format khác (ví dụ: without hyphens)
                    if (Guid.TryParseExact(senderIdClaim, "N", out senderId) ||
                        Guid.TryParseExact(senderIdClaim, "D", out senderId))
                    {
                        _logger.LogInformation("Parsed GUID with custom format: {SenderId}", senderId);
                    }
                    else
                    {
                        return Unauthorized(new
                        {
                            message = "User ID is not a valid GUID",
                            userId = senderIdClaim
                        });
                    }
                }

                _logger.LogInformation("Using senderId: {SenderId}", senderId);

                // Gọi service với đúng vai trò: senderId từ token, receiverId từ request body
                var result = await _friendService.SendFriendRequestAsync(senderId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending friend request");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách lời mời kết bạn đã nhận
        /// </summary>
        [HttpGet("requests/received")]
        public async Task<IActionResult> GetReceivedFriendRequests()
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }
                var requests = await _friendService.GetReceivedFriendRequestsAsync(userId);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting received requests");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách lời mời kết bạn đã gửi
        /// </summary>
        [HttpGet("requests/sent")]
        public async Task<IActionResult> GetSentFriendRequests()
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }
                var requests = await _friendService.GetSentFriendRequestsAsync(userId);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sent requests");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Chấp nhận lời mời kết bạn
        /// </summary>
        [HttpPost("requests/{requestId}/accept")]
        public async Task<IActionResult> AcceptFriendRequest(Guid requestId)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }
                var result = await _friendService.AcceptFriendRequestAsync(userId, requestId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting friend request");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Từ chối lời mời kết bạn
        /// </summary>
        [HttpPost("requests/{requestId}/reject")]
        public async Task<IActionResult> RejectFriendRequest(Guid requestId)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }
                var result = await _friendService.RejectFriendRequestAsync(userId, requestId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting friend request");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Hủy lời mời kết bạn đã gửi
        /// </summary>
        [HttpDelete("requests/{requestId}")]
        public async Task<IActionResult> CancelFriendRequest(Guid requestId)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }
                var result = await _friendService.CancelFriendRequestAsync(userId, requestId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling friend request");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Unfriend
        /// </summary>
        [HttpDelete("{friendId}")]
        public async Task<IActionResult> Unfriend(Guid friendId)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }
                var result = await _friendService.UnfriendAsync(userId, friendId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unfriending");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Block user
        /// </summary>
        
    }
}