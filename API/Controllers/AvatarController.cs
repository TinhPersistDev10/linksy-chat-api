using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.DTOs.Responses.Users;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace linksy_backend_api.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class AvatarController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IChatroomService _chatroomService;
        private readonly ILogger<AvatarController> _logger;

        public AvatarController(
            IUserService userService,
            IChatroomService chatroomService,
            ILogger<AvatarController> logger)
        {
            _userService = userService;
            _chatroomService = chatroomService;
            _logger = logger;
        }

        #region User Avatar

        /// <summary>
        /// Upload/Update user avatar
        /// POST /api/avatar/user
        /// </summary>
        [HttpPost("user")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<AvatarResponse>> UpdateUserAvatar(IFormFile avatar)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new AvatarResponse
                    {
                        Success = false,
                        Message = "Invalid user ID"
                    });
                }

                if (avatar == null)
                {
                    return BadRequest(new AvatarResponse
                    {
                        Success = false,
                        Message = "Avatar file is required"
                    });
                }

                var result = await _userService.UpdateUserAvatarAsync(userId, avatar);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user avatar");
                return BadRequest(new AvatarResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }


        /// Delete user avatar
        /// DELETE /api/avatar/user
        [HttpDelete("user")]
        public async Task<ActionResult<AvatarResponse>> DeleteUserAvatar()
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new AvatarResponse
                    {
                        Success = false,
                        Message = "Invalid user ID"
                    });
                }

                var result = await _userService.DeleteUserAvatarAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user avatar");
                return BadRequest(new AvatarResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        #endregion

        #region Group Avatar

        /// <summary>
        /// Upload/Update group avatar
        /// POST /api/avatar/group/{chatroomId}
        /// </summary>
        [HttpPost("group/{chatroomId}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<AvatarResponse>> UpdateGroupAvatar(
            Guid chatroomId,
            IFormFile avatar) // ✅ Bỏ [FromForm]
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new AvatarResponse
                    {
                        Success = false,
                        Message = "Invalid user ID"
                    });
                }

                if (avatar == null)
                {
                    return BadRequest(new AvatarResponse
                    {
                        Success = false,
                        Message = "Avatar file is required"
                    });
                }

                var result = await _chatroomService.UpdateGroupAvatarAsync(userId, chatroomId, avatar);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group avatar");
                return BadRequest(new AvatarResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete group avatar
        /// DELETE /api/avatar/group/{chatroomId}
        /// </summary>
        [HttpDelete("group/{chatroomId}")]
        public async Task<ActionResult<AvatarResponse>> DeleteGroupAvatar(Guid chatroomId)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new AvatarResponse
                    {
                        Success = false,
                        Message = "Invalid user ID"
                    });
                }

                var result = await _chatroomService.DeleteGroupAvatarAsync(userId, chatroomId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting group avatar");
                return BadRequest(new AvatarResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        #endregion
    }
}