using linksy_backend_api.Domain.DTOs.Responses.ContentModeration;
using linksy_backend_api.Domain.Interfaces.Services;
using linksy_backend_api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace linksy_backend_api.API.Controllers
{
    /// <summary>
    /// Cấu hình lọc từ khóa vi phạm dành cho client (chỉ để chặn sớm phía UI —
    /// backend vẫn là lớp thực thi cuối cùng ở MessageService/ScheduledMessageService).
    /// </summary>
    [ApiController]
    [Route("api/v1/content-moderation")]
    [Authorize]
    public class ContentModerationController : BaseApiController
    {
        private readonly IContentModerationSettingsService _settingsService;

        public ContentModerationController(IContentModerationSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        /// <summary>
        /// GET /api/v1/content-moderation/config
        /// </summary>
        [HttpGet("config")]
        public async Task<ActionResult<ApiResponseDto>> GetConfig()
        {
            var settings = await _settingsService.GetAsync();

            return Ok(new ApiResponseDto
            {
                Success = true,
                Message = "Content moderation config retrieved.",
                Data = new ContentModerationConfigDto
                {
                    Enabled = settings.Enabled,
                    BannedWords = settings.BannedWords
                }
            });
        }
    }
}
