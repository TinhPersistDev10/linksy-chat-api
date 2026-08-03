using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.MessagesDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace linksy_backend_api.API.Controllers
{
    [ApiController]
    [Route("api/v1/messages/{messageId:guid}/poll")]
    [Authorize]
    public class PollsController : BaseApiController
    {
        private readonly IPollService _pollService;
        private readonly ILogger<PollsController> _logger;

        public PollsController(IPollService pollService, ILogger<PollsController> logger)
        {
            _pollService = pollService;
            _logger = logger;
        }

        [HttpPost("vote")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        public async Task<IActionResult> Vote(Guid messageId, [FromBody] VotePollRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponseDto
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ."
                });

            try
            {
                var poll = await _pollService.VoteAsync(CurrentUserId, messageId, request);
                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Đã bình chọn.",
                    Data = poll
                });
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
                return BadRequest(new ApiResponseDto { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponseDto { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error voting poll MessageId={MessageId}", messageId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("close")]
        [ProducesResponseType(typeof(ApiResponseDto), 200)]
        public async Task<IActionResult> Close(Guid messageId)
        {
            try
            {
                var poll = await _pollService.CloseAsync(CurrentUserId, messageId);
                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "Đã hủy bình chọn.",
                    Data = poll
                });
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
                _logger.LogError(ex, "Error closing poll MessageId={MessageId}", messageId);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }
    }
}
