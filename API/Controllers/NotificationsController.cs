using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.DTOs.Requests.Notifications;
using linksy_backend_api.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace linksy_backend_api.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        private readonly IUserService _userService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(INotificationService notificationService, IUserService userService, ILogger<NotificationsController> logger)
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
                    message = "Có lỗi xảy ra khi lấy thông báo"
                });
            }   
        }
        
    }
}