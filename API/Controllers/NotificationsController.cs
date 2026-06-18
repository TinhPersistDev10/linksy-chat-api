using System;
using linksy_backend_api.Core.DTOs.Requests.Notifications;
using linksy_backend_api.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace linksy_backend_api.API.Controllers
{
    [ApiController]
    [Route("api/v1/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            INotificationService notificationService,
            IUserService userService,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _userService = userService;
            _logger = logger;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }

            return userId;
        }

        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetNotifications([FromQuery] GetNotificationsRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();

                _logger.LogInformation(
                    "User {UserId} getting notifications: Page={Page}, PageSize={PageSize}, Type={Type}, OnlyUnread={OnlyUnread}",
                    userId, request.Page, request.PageSize, request.NotificationType, request.OnlyUnread);

                var notifications = await _notificationService.GetUserNotificationsAsync(
                    userId,
                    request.Page,
                    request.PageSize);

                return Ok(new
                {
                    success = true,
                    data = notifications
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return StatusCode(500, new
                {
                    success = false,
                    message = "C? l?i x?y ra khi l?y th?ng b?o"
                });
            }
        }

        [HttpPost("{notificationId:guid}/read")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> MarkAsRead(Guid notificationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _notificationService.MarkAsReadAsync(userId, notificationId);
                return result.Success ? Ok(result) : NotFound(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
                return StatusCode(500, new { success = false, message = "C? l?i x?y ra khi ??nh d?u th?ng b?o ?? ??c" });
            }
        }

        [HttpPost("read-all")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _notificationService.MarkAllAsReadAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, new { success = false, message = "C? l?i x?y ra khi ??nh d?u t?t c? th?ng b?o ?? ??c" });
            }
        }

        [HttpDelete("{notificationId:guid}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteNotification(Guid notificationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _notificationService.DeleteNotificationAsync(userId, notificationId);
                return result.Success ? Ok(result) : NotFound(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {NotificationId}", notificationId);
                return StatusCode(500, new { success = false, message = "C? l?i x?y ra khi x?a th?ng b?o" });
            }
        }

        [HttpDelete]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteAllNotifications()
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _notificationService.DeleteAllNotificationsAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all notifications");
                return StatusCode(500, new { success = false, message = "C? l?i x?y ra khi x?a t?t c? th?ng b?o" });
            }
        }
    }
}
