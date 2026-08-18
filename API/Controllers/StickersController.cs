using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace linksy_backend_api.API.Controllers
{
    [ApiController]
    [Route("api/v1/stickers")]
    [Authorize]
    public class StickersController : BaseApiController
    {
        private readonly IStickerService _stickerService;
        private readonly ILogger<StickersController> _logger;

        public StickersController(IStickerService stickerService, ILogger<StickersController> logger)
        {
            _stickerService = stickerService;
            _logger = logger;
        }

        [HttpPost]
        [EnableRateLimiting("upload")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponseDto>> Create([FromForm] IFormFile file)
        {
            try
            {
                var sticker = await _stickerService.CreateStickerAsync(CurrentUserId, file);
                return StatusCode(201, new ApiResponseDto
                {
                    Success = true,
                    Message = "Đã tạo sticker",
                    Data = sticker
                });
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
                _logger.LogError(ex, "Error creating sticker");
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponseDto>> GetMine()
        {
            try
            {
                var stickers = await _stickerService.GetMyStickersAsync(CurrentUserId);
                return Ok(new ApiResponseDto
                {
                    Success = true,
                    Message = "OK",
                    Data = stickers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing stickers");
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponseDto>> Delete(Guid id)
        {
            try
            {
                await _stickerService.DeleteStickerAsync(CurrentUserId, id);
                return Ok(new ApiResponseDto { Success = true, Message = "Đã xoá sticker", Data = true });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponseDto { Success = false, Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponseDto { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting sticker {Id}", id);
                return StatusCode(500, new ApiResponseDto { Success = false, Message = ex.Message });
            }
        }
    }
}
