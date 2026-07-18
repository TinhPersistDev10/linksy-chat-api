using linksy_backend_api.Domain.DTOs.Requests.Reacions;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace linksy_backend_api.API.Controllers
{
    /// <summary>
    /// Quản lý reactions (emoji) trên tin nhắn.
    /// </summary>
    [ApiController]
    [Route("api/v1/messages/{messageId:guid}/reactions")]
    [Authorize]
    public class ReactionsController : BaseApiController
    {
        private readonly IReactionService _reactionService;
        private readonly ILogger<ReactionsController> _logger;

        public ReactionsController(
            IReactionService reactionService,
            ILogger<ReactionsController> logger)
        {
            _reactionService = reactionService;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/messages/{messageId}/reactions
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Lấy tất cả reactions của một tin nhắn, gom nhóm theo emoji.</summary>
        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetReactions(Guid messageId)
        {
            try
            {
                var result = await _reactionService.GetMessageReactionsAsync(CurrentUserId, messageId);
                return Ok(new ApiResponseDto { Success = true, Message = "Reactions retrieved.", Data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponseDto { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reactions for message {MessageId}", messageId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/messages/{messageId}/reactions
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Toggle reaction trên tin nhắn.
        /// Nếu user chưa react emoji đó → thêm.
        /// Nếu đã react → xóa (toggle off).
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ToggleReaction(
            Guid messageId,
            [FromBody] ToggleReactionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponseDto
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ.",
                    Data = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });

            try
            {
                var result = await _reactionService.ToggleReactionAsync(CurrentUserId, messageId, request);
                return Ok(result);
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
                _logger.LogError(ex, "Error toggling reaction on message {MessageId}", messageId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/messages/reactions/batch
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Lấy reactions cho nhiều messages cùng lúc (batch query).</summary>
        [HttpPost("/api/v1/messages/reactions/batch")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        public async Task<IActionResult> GetBatchReactions([FromBody] List<Guid> messageIds)
        {
            if (messageIds is null || !messageIds.Any())
                return BadRequest(new ApiResponseDto { Success = false, Message = "Cần ít nhất 1 messageId." });

            if (messageIds.Count > 100)
                return BadRequest(new ApiResponseDto { Success = false, Message = "Tối đa 100 messages mỗi lần." });

            try
            {
                var result = await _reactionService.GetBatchReactionsAsync(CurrentUserId, messageIds);
                return Ok(new ApiResponseDto { Success = true, Message = "Batch reactions retrieved.", Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batch reactions");
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }
    }
}