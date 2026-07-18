using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Domain.DTOs.Requests.MessageAttachment;
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
        private readonly IFileService _fileService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MessageController> _logger;

        public MessageController(
            IMessageService messageService,
            IFileService fileService,
            IUnitOfWork unitOfWork,
            ILogger<MessageController> logger)
        {
            _messageService = messageService;
            _fileService = fileService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        private Guid CurrentUserId =>
            Guid.Parse(User.FindFirstValue("user_id")
                ?? throw new UnauthorizedAccessException("Missing user_id claim"));

        [HttpGet("{chatroomId:guid}")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetMessages(
            Guid chatroomId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] Guid? beforeMessageId = null)
        {
            try
            {
                if (pageSize < 1) pageSize = 50;
                if (pageSize > 100) pageSize = 100; // hard cap

                if (beforeMessageId.HasValue)
                {
                    var (beforeMessages, hasMoreBefore) = await _messageService.GetMessagesBeforeAsync(
                        CurrentUserId,
                        chatroomId,
                        beforeMessageId.Value,
                        pageSize);

                    return Ok(new ApiResponseDto
                    {
                        Success = true,
                        Message = "Messages retrieved",
                        Data = new
                        {
                            Messages = beforeMessages,
                            Page = 0,
                            PageSize = pageSize,
                            HasMore = hasMoreBefore
                        }
                    });
                }

                if (page < 1) page = 1;

                var messages = await _messageService.GetMessagesAsync(CurrentUserId, chatroomId, page, pageSize);
                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Messages retrieved",
                    Data = new
                    {
                        Messages = messages,
                        Page = page,
                        PageSize = pageSize,
                        HasMore = messages.Count() == pageSize
                    }
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for chatroom {ChatroomId}", chatroomId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("{chatroomId:guid}/around/{messageId:guid}")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetMessagesAround(
            Guid chatroomId,
            Guid messageId,
            [FromQuery] int before = 20,
            [FromQuery] int after = 15)
        {
            try
            {
                if (before < 0) before = 0;
                if (after < 0) after = 0;
                if (before > 50) before = 50;
                if (after > 50) after = 50;

                var (messages, hasMoreBefore) = await _messageService.GetMessagesAroundAsync(
                    CurrentUserId,
                    chatroomId,
                    messageId,
                    before,
                    after);

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Messages around target retrieved",
                    Data = new
                    {
                        Messages = messages,
                        HasMoreBefore = hasMoreBefore,
                        TargetMessageId = messageId
                    }
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponseDto { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting messages around {MessageId} in chatroom {ChatroomId}",
                    messageId,
                    chatroomId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("{chatroomId:guid}/pinned")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetPinnedMessages(Guid chatroomId)
        {
            try
            {
                var pinned = await _messageService.GetPinnedMessagesAsync(CurrentUserId, chatroomId);
                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Pinned messages retrieved",
                    Data = pinned
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pinned messages for chatroom {ChatroomId}", chatroomId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

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

                if (limit < 1) limit = 1;
                if (limit > 100) limit = 100;

                // Verify the caller is a member of this chatroom
                var isMember = await _unitOfWork.ChatroomMembers.AnyAsync(
                    cm => cm.ChatroomId == chatroomId &&
                          cm.UserId == CurrentUserId &&
                          cm.LeftAt == null);

                if (!isMember)
                    return Forbid();

                var messages = await _unitOfWork.MessageRepository
                    .SearchMessageAsync(chatroomId, keyword, limit);

                var userId = CurrentUserId;
                var response = new List<MessageResponse>();
                foreach (var m in messages)
                    response.Add(await MessageMapper.ToResponseAsync(m, _unitOfWork, userId));

                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = $"Found {response.Count} result(s)",
                    Data = new
                    {
                        Keyword = keyword,
                        Results = response,
                        Count = response.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching messages in chatroom {ChatroomId}", chatroomId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

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
            catch (UnauthorizedAccessException)
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
            catch (ArgumentException ex)
            {
                _logger.LogWarning(
                    "Invalid REST edit request. MessageId={MessageId}, UserId={UserId}",
                    messageId,
                    CurrentUserId
                );

                return BadRequest(new ApiResponseDto
                {
                    Success = false,
                    Message = ex.Message
                });
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
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    "Attempted to delete an already deleted message. MessageId={MessageId}, UserId={UserId}",
                    messageId,
                    CurrentUserId
                );

                return Conflict(new ApiResponseDto
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error deleting MessageId={MessageId}, UserId={UserId}",
                    messageId,
                    CurrentUserId
                );

                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "Không thể xóa tin nhắn."
                });
            }
        }

        [HttpGet("{messageId:guid}/replies")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        public async Task<IActionResult> GetReplies(Guid messageId)
        {
            try
            {
                var replies = await _messageService.GetRepliesAsync(CurrentUserId, messageId);
                return Ok(new ApiResponseDto { Success = true, Message = "Replies retrieved", Data = replies });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponseDto
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error getting replies. MessageId={MessageId}, UserId={UserId}",
                    messageId,
                    CurrentUserId
                );

                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "Không thể tải danh sách trả lời."
                });
            }
        }

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

        [HttpPost("attachments/upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAttachment(
    [FromForm] UploadAttachmentRequest request)
        {
            try
            {
                var uploaded = await _messageService.UploadAttachmentAsync(CurrentUserId, request.ChatroomId, request.File, request.AttachmentType);
                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Attachment uploaded",
                    Data = uploaded
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponseDto
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading attachment.");
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "Cannot upload attachment."
                });
            }
        }
    }
}
