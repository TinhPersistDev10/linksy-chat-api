using linksy_backend_api.Core.DTOs.AdminDTOs;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Domain.DTOs.Requests.Users;
using linksy_backend_api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace linksy_backend_api.API.Controllers
{
    /// <summary>
    /// Quản lý thông tin cá nhân và tìm kiếm người dùng.
    /// </summary>
    [ApiController]
    [Route("api/v1/users")]
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/users/profile   — current user
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Lấy thông tin profile của user đang đăng nhập.</summary>
        [HttpGet("profile")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var result = await _userService.GetCurrentUserAsync(CurrentUserId);
                return Ok(new ApiResponseDto { Success = true, Message = "Profile retrieved.", Data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponseDto { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile for {UserId}", CurrentUserId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /api/users/profile
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Cập nhật thông tin cá nhân (username, fullname, bio, dob).</summary>
        [HttpPut("profile")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponseDto
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ.",
                    Data = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });

            try
            {
                // Reuse the admin DTO (same fields minus isActive/isEmailVerified)
                var dto = new UpdateUserByAdminDto
                {
                    Username = request.Username,
                    Fullname = request.Fullname,
                    Bio = request.Bio,
                    DateOfBirth = request.DateOfBirth
                };

                var result = await _userService.UpdateUserAsync(CurrentUserId, dto);
                return Ok(new ApiResponseDto { Success = true, Message = "Profile cập nhật thành công.", Data = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponseDto { Success = false, Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponseDto { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for {UserId}", CurrentUserId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchUsers([FromQuery] string query, [FromQuery] int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new ApiResponseDto
                {
                    Success = false,
                    Message = "Từ khóa tìm kiếm là bắt buộc",
                });
            }
            query = query.Trim();
            limit = Math.Clamp(limit, 1, 50);
            try
            {
                var users = await _userService.SearchUsersAsync(
                    CurrentUserId,
                    query,
                    limit);

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Tìm kiếm người dùng thành công.",
                    Data = users
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error searching users. RequestedBy={UserId}, Query={Query}",
                    CurrentUserId,
                    query);

                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "Không thể tìm kiếm người dùng."
                });
            }
        }
        // ─────────────────────────────────────────────────────────────────────
        // POST /api/users/avatar
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Upload / thay avatar.</summary>
        [HttpPost("avatar")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdateAvatar(IFormFile avatar)
        {
            if (avatar is null)
                return BadRequest(new ApiResponseDto { Success = false, Message = "File avatar là bắt buộc." });

            try
            {
                var result = await _userService.UpdateUserAvatarAsync(CurrentUserId, avatar);
                return result.Success
                    ? Ok(new ApiResponseDto { Success = true, Message = result.Message, Data = new { result.AvatarUrl } })
                    : BadRequest(new ApiResponseDto { Success = false, Message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating avatar for {UserId}", CurrentUserId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE /api/users/avatar
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Xóa avatar, đặt lại về mặc định.</summary>
        [HttpDelete("avatar")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        public async Task<IActionResult> DeleteAvatar()
        {
            try
            {
                var result = await _userService.DeleteUserAvatarAsync(CurrentUserId);
                return Ok(new ApiResponseDto { Success = result.Success, Message = result.Message, Data = new { result.AvatarUrl } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting avatar for {UserId}", CurrentUserId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/users/{userId}   — public profile
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Xem profile công khai của người dùng khác.</summary>
        [HttpGet("{userId:guid}")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUserById(Guid userId)
        {
            try
            {
                var result = await _userService.GetCurrentUserAsync(userId);
                return Ok(new ApiResponseDto { Success = true, Message = "User retrieved.", Data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponseDto { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", userId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }
    }
}
