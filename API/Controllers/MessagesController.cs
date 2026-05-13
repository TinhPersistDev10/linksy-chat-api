using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.MessageDTO;
using linksy_backend_api.DTOs.MessagesDTOs;
using linksy_backend_api.Infrastructure.Mappers;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace linksy_backend_api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/v1/messages")]
    [Produces("application/json")]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly IUnitOfWork     _unitOfWork;
        private readonly ILogger<MessageController> _logger;

        public MessageController(
            IMessageService messageService,
            IUnitOfWork unitOfWork,
            ILogger<MessageController> logger)
        {
            _messageService = messageService;
            _unitOfWork     = unitOfWork;
            _logger         = logger;
        }

        private Guid CurrentUserId =>
            Guid.Parse(User.FindFirstValue("user_id")
                ?? throw new UnauthorizedAccessException("Missing user_id claim"));

        // ─────────────────────────────────────────────────────────────────────
        // GET messages for a chatroom (paginated)
        // GET /api/messages/{chatroomId}?page=1&pageSize=50
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet("{chatroomId:guid}")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetMessages(
            Guid chatroomId,
            [FromQuery] int page     = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (page < 1)     page     = 1;
                if (pageSize < 1) pageSize = 50;
                if (pageSize > 100) pageSize = 100; // hard cap

                var messages = await _messageService.GetMessagesAsync(CurrentUserId, chatroomId, page, pageSize);

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Messages retrieved",
                    Data    = new
                    {
                        Messages    = messages,
                        Page        = page,
                        PageSize    = pageSize,
                        HasMore     = messages.Count() == pageSize
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for chatroom {ChatroomId}", chatroomId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // SEARCH messages inside a chatroom
        // GET /api/messages/{chatroomId}/search?keyword=hello&limit=50
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet("{chatroomId:guid}/search")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> SearchMessages(
            Guid chatroomId,
            [FromQuery] string keyword,
            [FromQuery] int limit = 50)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return BadRequest(new ApiResponseDto
                    {
                        Success = false,
                        Message = "keyword is required"
                    });

                if (limit < 1)   limit = 1;
                if (limit > 100) limit = 100;

                // Verify the caller is a member of this chatroom
                var isMember = await _unitOfWork.ChatroomMembers.AnyAsync(
                    cm => cm.ChatroomId == chatroomId &&
                          cm.UserId     == CurrentUserId &&
                          cm.LeftAt     == null);

                if (!isMember)
                    return Forbid();

                var messages = await _unitOfWork.MessageRepository
                    .SearchMessageAsync(chatroomId, keyword, limit);

                var userId   = CurrentUserId;
                var response = new List<MessageResponse>();
                foreach (var m in messages)
                    response.Add(await MessageMapper.ToResponseAsync(m, _unitOfWork, userId));

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = $"Found {response.Count} result(s)",
                    Data    = new
                    {
                        Keyword  = keyword,
                        Results  = response,
                        Count    = response.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching messages in chatroom {ChatroomId}", chatroomId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // SEND a message
        // POST /api/messages
        // ─────────────────────────────────────────────────────────────────────

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                var message = await _messageService.SendMessageAsync(CurrentUserId, request);

                return CreatedAtAction(nameof(GetMessages),
                    new { chatroomId = request.ChatroomId },
                    new ApiResponseDto { Success = true, Message = "Message sent", Data = message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponseDto { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // EDIT a message
        // PUT /api/messages/{messageId}
        // ─────────────────────────────────────────────────────────────────────

        [HttpPut("{messageId:guid}")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> EditMessage(Guid messageId, [FromBody] EditMessageRequest request)
        {
            try
            {
                var updated = await _messageService.EditMessageAsync(CurrentUserId, messageId, request.MessageText);
                return Ok(new ApiResponseDto { Success = true, Message = "Message updated", Data = updated });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponseDto { Success = false, Message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponseDto { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing message {MessageId}", messageId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE (soft) a message
        // DELETE /api/messages/{messageId}
        // ─────────────────────────────────────────────────────────────────────

        [HttpDelete("{messageId:guid}")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteMessage(Guid messageId)
        {
            try
            {
                await _messageService.DeleteMessageAsync(CurrentUserId, messageId);
                return Ok(new ApiResponseDto { Success = true, Message = "Message deleted" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponseDto { Success = false, Message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId}", messageId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET replies to a message
        // GET /api/messages/{messageId}/replies
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet("{messageId:guid}/replies")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        public async Task<IActionResult> GetReplies(Guid messageId)
        {
            try
            {
                var replies = await _messageService.GetRepliesAsync(messageId);
                return Ok(new ApiResponseDto { Success = true, Message = "Replies retrieved", Data = replies });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting replies for message {MessageId}", messageId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // MARK a message as read
        // POST /api/messages/{chatroomId}/read/{messageId}
        // ─────────────────────────────────────────────────────────────────────

        [HttpPost("{chatroomId:guid}/read/{messageId:guid}")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        public async Task<IActionResult> MarkAsRead(Guid chatroomId, Guid messageId)
        {
            try
            {
                await _messageService.MarkMessageAsReadAsync(CurrentUserId, chatroomId, messageId);
                return Ok(new ApiResponseDto { Success = true, Message = "Message marked as read" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponseDto { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponseDto { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message as read");
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }
    }
}