using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.DTOs.AdminDTOs;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.Moderation;
using linksy_backend_api.DTOs.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace linksy_backend_api.API.Controllers
{
    [ApiController]
    [Route("api/v1/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseApiController
    {
        private readonly IAdminService _adminService;
        private readonly IUserReportService _reportService;
        private readonly IUserModerationService _moderationService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IAdminService adminService,
            IUserReportService reportService,
            IUserModerationService moderationService,
            ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _reportService = reportService;
            _moderationService = moderationService;
            _logger = logger;
        }

        private ActionResult<ApiResponseDto>? RejectIfSelf(Guid targetUserId, string action)
        {
            if (targetUserId == CurrentUserId)
            {
                return BadRequest(new ApiResponseDto
                {
                    Success = false,
                    Message = $"Cannot {action} your own account",
                    Data = null
                });
            }

            return null;
        }

        #region User Management

        /// <summary>
        /// Get all users with pagination and search
        /// GET /api/admin/users?page=1&pageSize=10&search=keyword
        /// </summary>
        [HttpGet("users")]
        public async Task<ActionResult<ApiResponseDto>> GetAllUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var result = await _adminService.GetAllUsersAsync(page, pageSize, search);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Get user detail by ID
        /// GET /api/admin/users/{userId}
        /// </summary>
        [HttpGet("users/{userId}")]
        public async Task<ActionResult<ApiResponseDto>> GetUserDetail(Guid userId)
        {
            var result = await _adminService.GetUserDetailAsync(userId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// Create new user
        /// POST /api/admin/users
        /// </summary>
        [HttpPost("users")]
        public async Task<ActionResult<ApiResponseDto>> CreateUser([FromBody] CreateUserByAdminDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponseDto
                {
                    Success = false,
                    Message = "Invalid data",
                    Data = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            var result = await _adminService.CreateUserAsync(dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Update user information
        /// PUT /api/admin/users/{userId}
        /// </summary>
        [HttpPut("users/{userId}")]
        public async Task<ActionResult<ApiResponseDto>> UpdateUser(
            Guid userId,
            [FromBody] UpdateUserByAdminDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponseDto
                {
                    Success = false,
                    Message = "Invalid data",
                    Data = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            if (dto.IsActive == false)
            {
                var selfReject = RejectIfSelf(userId, "deactivate");
                if (selfReject != null)
                    return selfReject;
            }

            var result = await _adminService.UpdateUserAsync(userId, dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Delete user permanently
        /// DELETE /api/admin/users/{userId}
        /// </summary>
        [HttpDelete("users/{userId}")]
        public async Task<ActionResult<ApiResponseDto>> DeleteUser(Guid userId)
        {
            var selfReject = RejectIfSelf(userId, "delete");
            if (selfReject != null)
                return selfReject;

            var result = await _adminService.DeleteUserAsync(userId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Toggle user active status (activate/deactivate)
        /// PATCH /api/admin/users/{userId}/toggle-status
        /// </summary>
        [HttpPatch("users/{userId}/toggle-status")]
        public async Task<ActionResult<ApiResponseDto>> ToggleUserStatus(Guid userId)
        {
            var selfReject = RejectIfSelf(userId, "change status of");
            if (selfReject != null)
                return selfReject;

            var result = await _adminService.ToggleUserStatusAsync(userId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Reset user password
        /// POST /api/admin/users/{userId}/reset-password
        /// </summary>
        [HttpPost("users/{userId}/reset-password")]
        public async Task<ActionResult<ApiResponseDto>> ResetUserPassword(
            Guid userId,
            [FromBody] ResetPasswordDto dto)
        {
            if (userId != dto.UserId)
            {
                return BadRequest(new ApiResponseDto
                {
                    Success = false,
                    Message = "User ID mismatch",
                    Data = null ?? new ApiResponseDto
                    { Success = false, Message = "Reset Password error" }
                });
            }

            if (string.IsNullOrWhiteSpace(dto.NewPassword) ||
                dto.NewPassword.Length < 8 ||
                !dto.NewPassword.Any(char.IsLetter) ||
                !dto.NewPassword.Any(char.IsDigit))
            {
                return BadRequest(new ApiResponseDto
                {
                    Success = false,
                    Message = "Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ và số",
                    Data = null ?? new ApiResponseDto
                    { Success = false, Message = "Reset Password error" }
                });
            }

            var result = await _adminService.ResetUserPasswordAsync(userId, dto.NewPassword);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        #endregion

        #region Role Management

        /// <summary>
        /// Get all available roles
        /// GET /api/v1/admin/roles
        /// </summary>
        [HttpGet("roles")]
        public async Task<ActionResult<ApiResponseDto>> GetAllRoles()
        {
            var result = await _adminService.GetAllRolesAsync();
            return Ok(result);
        }

        /// <summary>
        /// Get user's assigned roles
        /// GET /api/v1/admin/users/{userId}/roles
        /// </summary>
        [HttpGet("users/{userId}/roles")]
        public async Task<ActionResult<ApiResponseDto>> GetUserRoles(Guid userId)
        {
            var result = await _adminService.GetUserRolesAsync(userId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// Assign role to user
        /// POST /api/v1/admin/users/assign-role
        /// Body: { "userId": "guid", "roleId": 1 }
        /// </summary>
        [HttpPost("users/assign-role")]
        public async Task<ActionResult<ApiResponseDto>> AssignRole([FromBody] AssignRoleDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponseDto
                {
                    Success = false,
                    Message = "Invalid data",
                    Data = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            var result = await _adminService.AssignRoleAsync(dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Remove role from user
        /// DELETE /api/v1/admin/users/{userId}/roles/{roleId}
        /// </summary>
        [HttpDelete("users/{userId}/roles/{roleId}")]
        public async Task<ActionResult<ApiResponseDto>> RemoveRole(Guid userId, int roleId)
        {
            var selfReject = RejectIfSelf(userId, "remove role from");
            if (selfReject != null)
                return selfReject;

            var result = await _adminService.RemoveRoleAsync(userId, roleId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        #endregion

        #region Statistics & Analytics

        /// <summary>
        /// Get admin dashboard statistics
        /// GET /api/v1/admin/statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponseDto>> GetStatistics()
        {
            var result = await _adminService.GetStatisticsAsync();
            return Ok(result);
        }

        /// <summary>
        /// Get recent activities
        /// GET /api/v1/admin/activities/recent?limit=10
        /// </summary>
        [HttpGet("activities/recent")]
        public async Task<ActionResult<ApiResponseDto>> GetRecentActivities(
            [FromQuery] int limit = 10)
        {
            if (limit < 1 || limit > 100)
            {
                return BadRequest(new ApiResponseDto
                {
                    Success = false,
                    Message = "Limit must be between 1 and 100",
                    Data = null
                });
            }

            var result = await _adminService.GetRecentActivitiesAsync(limit);
            return Ok(result);
        }

        /// <summary>
        /// Registration counts filtered by day / month / year
        /// GET /api/v1/admin/statistics/registrations?period=day|month|year&amp;from=&amp;to=
        /// </summary>
        [HttpGet("statistics/registrations")]
        public async Task<ActionResult<ApiResponseDto>> GetRegistrationStats(
            [FromQuery] string period = "day",
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var result = await _adminService.GetRegistrationStatsAsync(period, from, to);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        #endregion

        #region User Reports

        /// <summary>
        /// List account reports for moderation
        /// GET /api/v1/admin/reports?status=pending&amp;page=1&amp;pageSize=20
        /// </summary>
        [HttpGet("reports")]
        public async Task<ActionResult<ApiResponseDto>> GetReports(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _reportService.GetReportsForAdminAsync(status, page, pageSize);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/v1/admin/reports/{reportId}
        /// </summary>
        [HttpGet("reports/{reportId:guid}")]
        public async Task<ActionResult<ApiResponseDto>> GetReport(Guid reportId)
        {
            var result = await _reportService.GetReportByIdAsync(reportId);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Update report status (review / resolve / dismiss)
        /// PATCH /api/v1/admin/reports/{reportId}
        /// </summary>
        [HttpPatch("reports/{reportId:guid}")]
        public async Task<ActionResult<ApiResponseDto>> UpdateReport(
            Guid reportId,
            [FromBody] UpdateReportStatusRequest request)
        {
            var result = await _reportService.UpdateReportStatusAsync(
                CurrentUserId, reportId, request);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        #endregion

        #region User Moderation

        /// <summary>
        /// Apply / lift moderation on a user
        /// POST /api/v1/admin/users/{userId}/moderation
        /// </summary>
        [HttpPost("users/{userId:guid}/moderation")]
        public async Task<ActionResult<ApiResponseDto>> ApplyModeration(
            Guid userId,
            [FromBody] ApplyModerationRequest request)
        {
            var selfReject = RejectIfSelf(userId, "moderate");
            if (selfReject != null)
                return selfReject;

            var result = await _moderationService.ApplyAsync(CurrentUserId, userId, request);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        #endregion

        // #region Bulk Operations

        // /// <summary>
        // /// Bulk delete users
        // /// POST /api/admin/users/bulk-delete
        // /// Body: ["guid1", "guid2", "guid3"]
        // /// </summary>
        // [HttpPost("users/bulk-delete")]
        // public async Task<ActionResult<ApiResponseDto>> BulkDeleteUsers([FromBody] List<Guid> userIds)
        // {
        //     if (userIds == null || !userIds.Any())
        //     {
        //         return BadRequest(new ApiResponseDto
        //         {
        //             Success = false,
        //             Message = "No user IDs provided",
        //             Data = null
        //         });
        //     }

        //     int deletedCount = 0;
        //     var errors = new List<string>();

        //     foreach (var userId in userIds)
        //     {
        //         var result = await _adminService.DeleteUserAsync(userId);
        //         if (result.Success)
        //             deletedCount++;
        //         else
        //             errors.Add($"Failed to delete user {userId}: {result.Message}");
        //     }

        //     return Ok(new ApiResponseDto
        //     {
        //         Success = deletedCount > 0,
        //         Message = $"Successfully deleted {deletedCount} out of {userIds.Count} users",
        //         Data = new { DeletedCount = deletedCount, Errors = errors }
        //     });
        // }

        // /// <summary>
        // /// Bulk toggle user status
        // /// POST /api/admin/users/bulk-toggle-status
        // /// Body: ["guid1", "guid2", "guid3"]
        // /// </summary>
        // [HttpPost("users/bulk-toggle-status")]
        // public async Task<ActionResult<ApiResponseDto>> BulkToggleUserStatus([FromBody] List<Guid> userIds)
        // {
        //     if (userIds == null || !userIds.Any())
        //     {
        //         return BadRequest(new ApiResponseDto
        //         {
        //             Success = false,
        //             Message = "No user IDs provided",
        //             Data = null
        //         });
        //     }

        //     int processedCount = 0;
        //     var errors = new List<string>();

        //     foreach (var userId in userIds)
        //     {
        //         var result = await _adminService.ToggleUserStatusAsync(userId);
        //         if (result.Success)
        //             processedCount++;
        //         else
        //             errors.Add($"Failed to toggle user {userId}: {result.Message}");
        //     }

        //     return Ok(new ApiResponseDto
        //     {
        //         Success = processedCount > 0,
        //         Message = $"Successfully processed {processedCount} out of {userIds.Count} users",
        //         Data = new { ProcessedCount = processedCount, Errors = errors }
        //     });
        // }

        // #endregion
    }
}