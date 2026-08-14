using linksy_backend_api.API.Controllers;
using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.MessagesDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace linksy_backend_api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/scheduled-messages")]
public class ScheduledMessagesController : BaseApiController
{
    private readonly IScheduledMessageService _scheduledMessages;
    private readonly ILogger<ScheduledMessagesController> _logger;

    public ScheduledMessagesController(
        IScheduledMessageService scheduledMessages,
        ILogger<ScheduledMessagesController> logger)
    {
        _scheduledMessages = scheduledMessages;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponseDto>> Schedule([FromBody] ScheduleMessageRequest request)
    {
        try
        {
            var data = await _scheduledMessages.ScheduleAsync(CurrentUserId, request);
            return Ok(new ApiResponseDto
            {
                Success = true,
                Message = "Đã hẹn giờ tin nhắn",
                Data = data
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new ApiResponseDto { Success = false, Message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponseDto { Success = false, Message = ex.Message });
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
            _logger.LogError(ex, "Error scheduling message");
            return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponseDto>> GetPending([FromQuery] Guid chatroomId)
    {
        try
        {
            if (chatroomId == Guid.Empty)
                return BadRequest(new ApiResponseDto { Success = false, Message = "Thiếu chatroomId." });

            var data = await _scheduledMessages.GetPendingMineAsync(CurrentUserId, chatroomId);
            return Ok(new ApiResponseDto
            {
                Success = true,
                Message = "OK",
                Data = data
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new ApiResponseDto { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing scheduled messages");
            return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponseDto>> Cancel(Guid id)
    {
        try
        {
            await _scheduledMessages.CancelAsync(CurrentUserId, id);
            return Ok(new ApiResponseDto
            {
                Success = true,
                Message = "Đã hủy tin nhắn hẹn giờ",
                Data = true
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new ApiResponseDto { Success = false, Message = ex.Message });
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
            _logger.LogError(ex, "Error cancelling scheduled message {Id}", id);
            return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
        }
    }
}
