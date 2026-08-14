using linksy_backend_api.Core.Interfaces.Services;
using linksy_backend_api.DTOs.ChatroomDTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace linksy_backend_api.Controllers
{
    [Authorize]
    [EnableRateLimiting("api")]
    [ApiController]
    [Route("api/v1/invitations")]
    public class GroupInvitationsController : ControllerBase
    {
        private readonly IGroupInvitationService _invitationService;
        private readonly ILogger<GroupInvitationsController> _logger;

        public GroupInvitationsController(
            IGroupInvitationService invitationService,
            ILogger<GroupInvitationsController> logger)
        {
            _invitationService = invitationService;
            _logger            = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private bool TryGetUserId(out Guid userId)
        {
            userId = Guid.Empty;
            var claim = User.FindFirst("user_id")?.Value
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return !string.IsNullOrEmpty(claim) && Guid.TryParse(claim, out userId);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Lấy danh sách lời mời đã nhận (pending)</summary>
        [HttpGet("received")]
        public async Task<IActionResult> GetReceivedInvitations()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var invitations = await _invitationService.GetReceivedInvitationsAsync(userId);
                return Ok(invitations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting received invitations");
                return BadRequest(new { message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // SEND
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Gửi lời mời vào group</summary>
        [HttpPost("chatrooms/{chatroomId:guid}")]
        public async Task<IActionResult> SendGroupInvitation(Guid chatroomId, [FromBody] SendGroupInvitationRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var result = await _invitationService.SendGroupInvitationAsync(userId, chatroomId, request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (KeyNotFoundException ex)        { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending group invitation");
                return BadRequest(new { message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // ACCEPT / REJECT
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Chấp nhận lời mời</summary>
        [HttpPost("{invitationId:guid}/accept")]
        public async Task<IActionResult> AcceptInvitation(Guid invitationId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var result = await _invitationService.AcceptGroupInvitationAsync(userId, invitationId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting invitation");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Từ chối lời mời</summary>
        [HttpPost("{invitationId:guid}/reject")]
        public async Task<IActionResult> RejectInvitation(Guid invitationId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var result = await _invitationService.RejectGroupInvitationAsync(userId, invitationId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting invitation");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}