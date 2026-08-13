using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using linksy_backend_api.Domain.DTOs.Requests.MemberPermission;
using linksy_backend_api.Domain.DTOs.Responses.MemberPermission;
using linksy_backend_api.Domain.Enums;
using linksy_backend_api.Domain.Interfaces.Repositories;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.DTOs;
using linksy_backend_api.Repositories;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace linksy_backend_api.API.Controllers
{
    [Authorize]
    [EnableRateLimiting("api")]
    [ApiController]
    [Route("api/v1/chatrooms/{chatroomId:guid}/permissions")]
    [Produces("application/json")]
    public class MemberPermissionController : BaseApiController  // ✅ kế thừa BaseApiController
    {
        // ✅ Không còn IUnitOfWork — controller không truy cập repo trực tiếp
        private readonly IChatroomAccessService _access;
        private readonly IMemberPermissionService _permissionService;
        private readonly ILogger<MemberPermissionController> _logger;

        public MemberPermissionController(
            IChatroomAccessService access,
            IMemberPermissionService permissionService,
            ILogger<MemberPermissionController> logger)
        {
            _access = access;
            _permissionService = permissionService;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET all permissions  (admin only)
        // GET /api/chatrooms/{chatroomId}/permissions
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetAllPermissions(Guid chatroomId)
        {
            try
            {
                // ✅ CurrentUserId từ BaseApiController — không cần viết lại
                // ✅ Business logic gọi qua service — không nằm trong controller
                await _access.EnsureAdminAsync(chatroomId, CurrentUserId);

                var result = await _permissionService.GetAllAsync(chatroomId);
                return Ok(new ApiResponseDto { Success = true, Message = "Permissions retrieved", Data = result });
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for chatroom {ChatroomId}", chatroomId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET my own permissions
        // GET /api/chatrooms/{chatroomId}/permissions/me
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet("me")]
        public async Task<IActionResult> GetMyPermissions(Guid chatroomId)
        {
            try
            {
                await _access.EnsureMemberAsync(chatroomId, CurrentUserId);

                var result = await _permissionService.GetByUserAsync(chatroomId, CurrentUserId);
                return Ok(new ApiResponseDto { Success = true, Message = "Your permissions retrieved", Data = result });
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (KeyNotFoundException ex) { return NotFound(new ApiResponseDto { Success = false, Message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for user in chatroom {ChatroomId}", chatroomId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET a specific user's permissions  (admin only)
        // GET /api/chatrooms/{chatroomId}/permissions/{userId}
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> GetUserPermissions(Guid chatroomId, Guid userId)
        {
            try
            {
                await _access.EnsureAdminAsync(chatroomId, CurrentUserId);

                var result = await _permissionService.GetByUserAsync(chatroomId, userId);
                return Ok(new ApiResponseDto { Success = true, Message = "Permissions retrieved", Data = result });
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (KeyNotFoundException ex) { return NotFound(new ApiResponseDto { Success = false, Message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // CHECK my own single permission
        // POST /api/chatrooms/{chatroomId}/permissions/check-mine
        // ─────────────────────────────────────────────────────────────────────

        // Trong MemberPermissionController:
        [HttpPost("check-mine")]
        public async Task<IActionResult> CheckMyPermission(
            Guid chatroomId,
            [FromBody] CheckMyPermissionRequest request)
        {
            // Parse string → enum tại controller boundary
            if (!Enum.TryParse<PermissionType>(request.PermissionType, out var permissionType))
                return BadRequest(new ApiResponseDto
                {
                    Success = false,
                    Message = $"PermissionType '{request.PermissionType}' không hợp lệ. " +
                              $"Các giá trị hợp lệ: {string.Join(", ", Enum.GetNames<PermissionType>())}"
                });

            await _access.EnsureMemberAsync(chatroomId, CurrentUserId);

            var has = await _access.HasPermissionAsync(chatroomId, CurrentUserId, permissionType);

            return Ok(new PermissionCheckResponse
            {
                HasPermission = has,
                PermissionType = request.PermissionType,
                Message = has ? "Permission granted" : "Permission denied"
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // CHECK multiple permissions at once
        // POST /api/chatrooms/{chatroomId}/permissions/check-mine/batch
        // ─────────────────────────────────────────────────────────────────────

        [HttpPost("check-mine/batch")]
        public async Task<IActionResult> CheckMyMultiplePermissions(Guid chatroomId, [FromBody] CheckMultiplePermissionsRequest request)
        {
            try
            {
                await _access.EnsureMemberAsync(chatroomId, CurrentUserId);

                var results = new Dictionary<string, bool>();
                var invalidTypes = new List<string>();
                foreach (var perm in request.PermissionTypes)
                {
                    if (!Enum.TryParse<PermissionType>(perm, ignoreCase: true, out var permissionType))
                    {
                        invalidTypes.Add(perm);
                        results[perm] = false;
                        continue;
                    }

                    results[perm] = await _access.HasPermissionAsync(chatroomId, CurrentUserId, permissionType);

                }
                var denied = results.Where(kv => !kv.Value).Select(kv => kv.Key).ToList();
                return Ok(new MultiplePermissionCheckResponse
                {
                    Permissions = results,
                    AllGranted = denied.Count == 0,
                    DeniedPermissions = denied
                });
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking multiple permissions in chatroom {ChatroomId}", chatroomId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE a single member's permissions  (admin only)
        // PUT /api/chatrooms/{chatroomId}/permissions/{userId}
        // ─────────────────────────────────────────────────────────────────────

        [HttpPut("{userId:guid}")]
        public async Task<IActionResult> UpdatePermission(
            Guid chatroomId,
            Guid userId,
            [FromBody] UpdateMemberPermissionRequest request)
        {
            try
            {
                await _access.EnsureAdminAsync(chatroomId, CurrentUserId);

                if (CurrentUserId == userId)
                    return BadRequest(new ApiResponseDto
                    {
                        Success = false,
                        Message = "Admins cannot change their own permissions"
                    });

                await _permissionService.UpdateAsync(chatroomId, userId, request);
                return Ok(new ApiResponseDto { Success = true, Message = "Permissions updated" });
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (KeyNotFoundException ex) { return NotFound(new ApiResponseDto { Success = false, Message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permission for user {UserId}", userId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // BATCH UPDATE  (admin only)
        // PUT /api/chatrooms/{chatroomId}/permissions/batch
        // ─────────────────────────────────────────────────────────────────────

        [HttpPut("batch")]
        public async Task<IActionResult> BatchUpdatePermissions(
            Guid chatroomId,
            [FromBody] BatchUpdatePermissionRequest request)
        {
            try
            {
                await _access.EnsureAdminAsync(chatroomId, CurrentUserId);

                var result = await _permissionService.BatchUpdateAsync(chatroomId, CurrentUserId, request);
                return Ok(new ApiResponseDto { Success = true, Message = result.Message, Data = result });
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch-updating permissions in chatroom {ChatroomId}", chatroomId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // MUTE / UNMUTE  (admin only)
        // POST /api/chatrooms/{chatroomId}/permissions/{userId}/mute
        // POST /api/chatrooms/{chatroomId}/permissions/{userId}/unmute
        // ─────────────────────────────────────────────────────────────────────

        [HttpPost("{userId:guid}/mute")]
        public async Task<IActionResult> MuteMember(
            Guid chatroomId,
            Guid userId,
            [FromBody] MuteMemberRequest request)
        {
            try
            {
                await _access.EnsureAdminAsync(chatroomId, CurrentUserId);

                if (CurrentUserId == userId)
                    return BadRequest(new ApiResponseDto { Success = false, Message = "Cannot mute yourself" });

                var result = await _permissionService.MuteMemberAsync(chatroomId, userId, request);
                return Ok(
                    new ApiResponseDto
                    {
                        Success = true,
                        Message = result.Message,
                        Data = result.Data ?? new ApiResponseDto { Success = false, Message = " error" }
                    });
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (KeyNotFoundException ex) { return NotFound(new ApiResponseDto { Success = false, Message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error muting member {UserId}", userId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("{userId:guid}/unmute")]
        public async Task<IActionResult> UnmuteMember(Guid chatroomId, Guid userId)
        {
            try
            {
                await _access.EnsureAdminAsync(chatroomId, CurrentUserId);
                await _permissionService.UnmuteMemberAsync(chatroomId, userId);
                return Ok(new ApiResponseDto { Success = true, Message = "Member unmuted" });
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (KeyNotFoundException ex) { return NotFound(new ApiResponseDto { Success = false, Message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unmuting member {UserId}", userId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }
    }
}