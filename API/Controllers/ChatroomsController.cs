using linksy_backend_api.DTOs.ChatroomDTO;
using linksy_backend_api.Services.IServices;
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
    [Route("api/v1/chatrooms")]
    public class ChatroomsController : ControllerBase
    {
        private readonly IChatroomService _chatroomService;
        private readonly ILogger<ChatroomsController> _logger;

        public ChatroomsController(IChatroomService chatroomService, ILogger<ChatroomsController> logger)
        {
            _chatroomService = chatroomService;
            _logger = logger;
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

        [HttpGet]
        public async Task<IActionResult> GetChatrooms(
     [FromQuery] bool includeArchived = false,
     [FromQuery] string? type = null)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var chatrooms = await _chatroomService.GetUserChatroomsAsync(userId, includeArchived, type);
                return Ok(chatrooms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chatrooms");
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("{chatroomId:guid}")]
        public async Task<IActionResult> GetChatroomDetails(Guid chatroomId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var chatroom = await _chatroomService.GetChatroomDetailsAsync(userId, chatroomId);
                return Ok(chatroom);
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chatroom details");
                return BadRequest(new { message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREATE
        // ─────────────────────────────────────────────────────────────────────

        [HttpPost("direct")]
        public async Task<IActionResult> CreateDirectChatroom([FromBody] CreateDirectChatRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var chatroom = await _chatroomService.CreateDirectChatroomAsync(userId, request.OtherUserId);
                return Ok(chatroom);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating direct chatroom");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("group")]
        public async Task<IActionResult> CreateGroupChatroom([FromBody] CreateGroupChatroomRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var chatroom = await _chatroomService.CreateGroupChatroomAsync(userId, request);
                return CreatedAtAction(nameof(GetChatroomDetails), new { chatroomId = chatroom.ChatroomId }, chatroom);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group chatroom");
                return BadRequest(new { message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────────────────────────────────

        [HttpPut("{chatroomId:guid}")]
        public async Task<IActionResult> UpdateGroupInfo(Guid chatroomId, [FromBody] UpdateChatroomRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var chatroom = await _chatroomService.UpdateChatroomInfoAsync(userId, chatroomId, request);
                return Ok(chatroom);
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group info");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{chatroomId:guid}/avatar")]
        public async Task<IActionResult> UpdateGroupAvatar(Guid chatroomId, IFormFile avatarFile)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var result = await _chatroomService.UpdateGroupAvatarAsync(userId, chatroomId, avatarFile);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group avatar");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{chatroomId:guid}/avatar")]
        public async Task<IActionResult> DeleteGroupAvatar(Guid chatroomId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var result = await _chatroomService.DeleteGroupAvatarAsync(userId, chatroomId);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting group avatar");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{chatroomId:guid}/archive")]
        public async Task<IActionResult> ArchiveChatroom(Guid chatroomId, [FromBody] ArchiveChatroomRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var result = await _chatroomService.ArchiveChatroomAsync(userId, chatroomId, request.IsArchived);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving chatroom");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{chatroomId:guid}/pin")]
        public async Task<IActionResult> PinChatroom(Guid chatroomId, [FromBody] PinChatroomRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var result = await _chatroomService.PinChatroomAsync(userId, chatroomId, request.IsPinned);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pinning chatroom");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{chatroomId:guid}/mute")]
        public async Task<IActionResult> MuteChatroom(Guid chatroomId, [FromBody] MuteConversationRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var result = await _chatroomService.MuteChatroomAsync(
                    userId, chatroomId, request.IsMuted, request.MuteUntil);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error muting chatroom");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Soft-delete conversation for the current user (hide history + remove from list until new activity).
        /// </summary>
        [HttpPost("{chatroomId:guid}/clear")]
        public async Task<IActionResult> ClearConversation(Guid chatroomId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var result = await _chatroomService.ClearConversationAsync(userId, chatroomId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing conversation");
                return BadRequest(new { message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // MEMBERS
        // ─────────────────────────────────────────────────────────────────────

        [HttpPost("{chatroomId:guid}/members")]
        public async Task<IActionResult> AddMembers(Guid chatroomId, [FromBody] AddMembersRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var result = await _chatroomService.AddMembersAsync(userId, chatroomId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding members");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{chatroomId:guid}/members/{memberId:guid}")]
        public async Task<IActionResult> RemoveMember(Guid chatroomId, Guid memberId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var result = await _chatroomService.RemoveMemberAsync(userId, chatroomId, memberId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing member");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{chatroomId:guid}/members/{memberId:guid}/permissions")]
        public async Task<IActionResult> UpdateMemberPermissions(Guid chatroomId, Guid memberId, [FromBody] UpdateMemberPermissionsRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var result = await _chatroomService.UpdateMemberPermissionsAsync(userId, chatroomId, memberId, request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating member permissions");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{chatroomId:guid}/members/{memberId:guid}/promote")]
        public async Task<IActionResult> PromoteMember(Guid chatroomId, Guid memberId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var result = await _chatroomService.PromoteToAdminAsync(userId, chatroomId, memberId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error promoting member");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{chatroomId:guid}/members/{memberId:guid}/demote")]
        public async Task<IActionResult> DemoteMember(Guid chatroomId, Guid memberId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var result = await _chatroomService.DemoteFromAdminAsync(userId, chatroomId, memberId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error demoting member");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{chatroomId:guid}")]
        public async Task<IActionResult> DisbandChatroom(Guid chatroomId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var result = await _chatroomService.DisbandChatroomAsync(userId, chatroomId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disbanding chatroom");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{chatroomId:guid}/leave")]
        public async Task<IActionResult> LeaveChatroom(Guid chatroomId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid token." });

            try
            {
                var result = await _chatroomService.LeaveChatroomAsync(userId, chatroomId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving chatroom");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}