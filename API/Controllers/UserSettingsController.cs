using linksy_backend_api.Domain.DTOs.Requests.Settings;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace linksy_backend_api.API.Controllers
{
    /// <summary>
    /// Quản lý cài đặt người dùng: giao diện, thông báo, quyền riêng tư, trạng thái.
    /// </summary>
    [ApiController]
    [Route("api/v1/settings")]
    [Authorize]
    public class UserSettingsController : BaseApiController
    {
        private readonly IUserSettingsService _settingsService;
        private readonly ILogger<UserSettingsController> _logger;

        public UserSettingsController(
            IUserSettingsService settingsService,
            ILogger<UserSettingsController> logger)
        {
            _settingsService = settingsService;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/settings
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Lấy toàn bộ cài đặt của user hiện tại (1 request duy nhất).</summary>
        [HttpGet]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAllSettings()
        {
            try
            {
                var result = await _settingsService.GetAllSettingsAsync(CurrentUserId);
                return Ok(new ApiResponseDto { Success = true, Message = "Settings retrieved.", Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting settings for user {UserId}", CurrentUserId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /api/settings/general
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Cập nhật cài đặt giao diện (ngôn ngữ, múi giờ, theme).</summary>
        [HttpPut("general")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdateUserSettings([FromBody] UpdateUserSettingsRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(BuildValidationError());

            try
            {
                var result = await _settingsService.UpdateUserSettingsAsync(CurrentUserId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating general settings for {UserId}", CurrentUserId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /api/settings/notifications
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Cập nhật cài đặt thông báo.</summary>
        [HttpPut("notifications")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdateNotificationSettings(
            [FromBody] UpdateNotificationSettingsRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(BuildValidationError());

            try
            {
                var result = await _settingsService.UpdateNotificationSettingsAsync(CurrentUserId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification settings for {UserId}", CurrentUserId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /api/settings/privacy
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Cập nhật cài đặt quyền riêng tư.</summary>
        [HttpPut("privacy")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdatePrivacySettings([FromBody] UpdatePrivacySettingsRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(BuildValidationError());

            try
            {
                var result = await _settingsService.UpdatePrivacySettingsAsync(CurrentUserId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating privacy settings for {UserId}", CurrentUserId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /api/settings/status
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Cập nhật trạng thái hiện diện (online, away, busy, offline).</summary>
        [HttpPut("status")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdateUserStatus([FromBody] UpdateUserStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(BuildValidationError());

            try
            {
                var result = await _settingsService.UpdateUserStatusAsync(CurrentUserId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for {UserId}", CurrentUserId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPER
        // ─────────────────────────────────────────────────────────────────────

        private ApiResponseDto BuildValidationError() => new()
        {
            Success = false,
            Message = "Dữ liệu không hợp lệ.",
            Data = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
        };
    }
}