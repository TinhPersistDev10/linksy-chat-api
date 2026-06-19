using linksy_backend_api.Domain.DTOs.Responses.Delivery;
using linksy_backend_api.DTOs;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace linksy_backend_api.API.Controllers
{
    /// <summary>
    /// Theo dõi trạng thái giao/đọc của tin nhắn (sent → delivered → read).
    /// </summary>
    [ApiController]
    [Route("api/v1/messages")]
    [Authorize]
    public class MessageDeliveryController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MessageDeliveryController> _logger;
        private readonly IMessageService _messageService;

        public MessageDeliveryController(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ILogger<MessageDeliveryController> logger)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/messages/{messageId}/deliveries
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Lấy delivery status của một tin nhắn (chỉ sender hoặc admin mới được xem).</summary>
        [HttpGet("{messageId:guid}/deliveries")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetDeliveryStatus(Guid messageId)
        {
            try
            {
                var message = await _unitOfWork.Messages.GetByIdAsync(messageId);
                if (message is null)
                    return NotFound(new ApiResponseDto { Success = false, Message = "Không tìm thấy tin nhắn." });

                // Only the sender (or chatroom admin) may see full delivery info
                bool isSender = message.SenderId == CurrentUserId;
                bool isAdmin = await _unitOfWork.ChatroomMemberRepository
                    .GetActiveMemberAsync(message.ChatroomId, CurrentUserId)
                    .ContinueWith(t => t.Result?.MemberRole == "admin");

                if (!isSender && !isAdmin)
                    return Forbid();

                var deliveries = await _unitOfWork.MessageDeliveryRepository.GetByMessageAsync(messageId);

                var response = new MessageDeliveryStatusResponse
                {
                    MessageId = messageId,
                    SentCount = deliveries.Count,
                    DeliveredCount = deliveries.Count(d => d.Status is "delivered" or "read"),
                    ReadCount = deliveries.Count(d => d.Status == "read"),
                    Deliveries = deliveries.Select(d => new MessageDeliveryResponse
                    {
                        DeliveryId = d.DeliveryId,
                        MessageId = d.MessageId,
                        UserId = d.UserId,
                        Username = d.User?.Username ?? string.Empty,
                        Avatar = d.User?.Avatar,
                        Status = d.Status,
                        DeliveredAt = d.DeliveredAt,
                        ReadAt = d.ReadAt
                    }).ToList()
                };

                return Ok(new ApiResponseDto { Success = true, Message = "Delivery status retrieved.", Data = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting delivery status for {MessageId}", messageId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/messages/{messageId}/delivered
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Đánh dấu tin nhắn đã được nhận trên thiết bị (delivered).</summary>
        [HttpPost("{messageId:guid}/delivered")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        public async Task<IActionResult> MarkDelivered(Guid messageId)
        {
            try
            {
                await _messageService.MarkMessageAsDeliveredAsync(CurrentUserId, messageId);

                return Ok(new ApiResponseDto { Success = true, Message = "Đã đánh dấu đã nhận." });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex,
                    "Message not found while marking delivered. MessageId={MessageId}, UserId={UserId}",
                    messageId,
                    CurrentUserId);
                return NotFound(new ApiResponseDto { Success = false, Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex,
                    "Unauthorized delivered request. MessageId={MessageId}, UserId={UserId}",
                    messageId,
                    CurrentUserId);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error marking message delivered. MessageId={MessageId}, UserId={UserId}",
                    messageId,
                    CurrentUserId);
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "Không thể đánh dấu tin nhắn đã nhận."
                });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/messages/{chatroomId}/read-all
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Đánh dấu tất cả tin nhắn chưa đọc trong chatroom là đã đọc.</summary>
        [HttpPost("{chatroomId:guid}/read-all")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        public async Task<IActionResult> MarkAllRead(Guid chatroomId)
        {
            try
            {
                await _messageService.MarkAllMessagesAsReadAsync(CurrentUserId, chatroomId);

                return Ok(new ApiResponseDto { Success = true, Message = "Đã đánh dấu tất cả tin nhắn là đã đọc." });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex,
                    "Unauthorized read-all request. ChatroomId={ChatroomId}, UserId={UserId}",
                    chatroomId,
                    CurrentUserId);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error marking all messages as read. ChatroomId={ChatroomId}, UserId={UserId}",
                    chatroomId,
                    CurrentUserId);
                return StatusCode(500, new ApiResponseDto
                {
                    Success = false,
                    Message = "Không thể đánh dấu tin nhắn đã đọc."
                });
            }
        }
    }
}
