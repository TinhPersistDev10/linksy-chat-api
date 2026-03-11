using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.DTOs.MessageDTO;
using linksy_backend_api.DTOs.MessagesDTOs;
using Microsoft.AspNetCore.Mvc;

namespace linksy_backend_api.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(IMessageService messageService, ILogger<MessagesController> logger)
        {
            _messageService = messageService;
            _logger = logger;
        }

        [HttpGet("{chatroomId}")]
        public async Task<IActionResult> GetMessages(Guid chatroomId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id");
                if(userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                    return Unauthorized(new { message = "User ID claim is missing." });

                if(!Guid.TryParse(userIdClaim.Value, out var userId))
                    return BadRequest(new { message = "Invalid User ID format." });

                var messages = await _messageService.GetMessagesAsync(userId, chatroomId, page, pageSize);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages");
                return BadRequest(new { message = ex.Message });
            }
        }

         #region Messages

        /// <summary>
        /// Gửi message (HTTP fallback - SignalR được ưu tiên)
        /// </summary>
        [HttpPost("{chatroomId}")]
        public async Task<IActionResult> SendMessage(Guid chatroomId, [FromBody] SendMessageRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id");
                if(userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                    return Unauthorized(new { message = "User ID claim is missing." });

                if(!Guid.TryParse(userIdClaim.Value, out var userId))
                    return BadRequest(new { message = "Invalid User ID format." });

                request.ChatroomId = chatroomId;
                var message = await _messageService.SendMessageAsync(userId, request);
                return Ok(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Chỉnh sửa message
        /// </summary>
        [HttpPut("{messageId}")]
        public async Task<IActionResult> EditMessage(Guid messageId, [FromBody] EditMessageRequest request)
        {
            try
            {
               var userIdClaim = User.FindFirst("user_id");
                if(userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                    return Unauthorized(new { message = "User ID claim is missing." });

                if(!Guid.TryParse(userIdClaim.Value, out var userId))
                    return BadRequest(new { message = "Invalid User ID format." });

                var message = await _messageService.EditMessageAsync(userId, messageId, request.MessageText);
                return Ok(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing message");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa message
        /// </summary>
        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(Guid messageId)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id");
                if(userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                    return Unauthorized(new { message = "User ID claim is missing." });

                if(!Guid.TryParse(userIdClaim.Value, out var userId))
                    return BadRequest(new { message = "Invalid User ID format." });

                await _messageService.DeleteMessageAsync(userId, messageId);
                return Ok(new { success = true, message = "Đã xóa tin nhắn" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy replies của một message
        /// </summary>
        [HttpGet("messages/{messageId}/replies")]
        public async Task<IActionResult> GetReplies(Guid messageId)
        {
            try
            {
                var replies = await _messageService.GetRepliesAsync(messageId);
                return Ok(replies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting replies");
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion

    }
}