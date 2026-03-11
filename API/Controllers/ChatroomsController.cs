using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using linksy_backend_api.DTOs.ChatroomDTO;
using linksy_backend_api.DTOs.MessageDTO;
using linksy_backend_api.DTOs.MessagesDTOs;
using linksy_backend_api.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace linksy_backend_api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatroomsController : ControllerBase
    {
        private readonly IChatroomService _chatroomService;
        private readonly ILogger<ChatroomsController> _logger;

        public ChatroomsController(IChatroomService chatroomService, ILogger<ChatroomsController> logger)
        {
            _chatroomService = chatroomService;
            _logger = logger;
        }
        #region Chatrooms

       
        /// Lấy danh sách chatrooms của user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetChatrooms([FromQuery] bool includeArchived = false)
        {
            try
            {
                // Debug logging
                _logger.LogInformation("=== GetChatrooms called ===");
                _logger.LogInformation($"User authenticated: {User.Identity?.IsAuthenticated}");
                _logger.LogInformation($"User identity name: {User.Identity?.Name}");
                
                // Log all claims
                var allClaims = User.Claims.Select(c => $"{c.Type}={c.Value}");
                _logger.LogInformation($"All claims: {string.Join(", ", allClaims)}");
                
                // Thử nhiều cách lấy userId
                var userIdClaim = User.FindFirst("user_id")?.Value;
                var nameIdentifierClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var subClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                
                _logger.LogInformation($"user_id claim: {userIdClaim}");
                _logger.LogInformation($"NameIdentifier claim: {nameIdentifierClaim}");
                _logger.LogInformation($"sub claim: {subClaim}");
                
                // Lấy userId
                var userIdString = userIdClaim ?? nameIdentifierClaim ?? subClaim;
                
                if (string.IsNullOrEmpty(userIdString))
                {
                    _logger.LogError("No user_id claim found in token!");
                    return Unauthorized(new { message = "Invalid token - no user_id claim" });
                }
                
                if (!Guid.TryParse(userIdString, out var userId))
                {
                    _logger.LogError($"Cannot parse user_id: {userIdString}");
                    return BadRequest(new { message = "Invalid user_id format" });
                }
                
                _logger.LogInformation($"Getting chatrooms for user: {userId}");
                
                var chatrooms = await _chatroomService.GetUserChatroomsAsync(userId);
                
                _logger.LogInformation($"Found {chatrooms.Count} chatrooms");
                
                return Ok(chatrooms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chatrooms");
                return BadRequest(new { message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        
        /// Lấy chi tiết chatroom
        
        [HttpGet("{chatroomId}")]
        public async Task<IActionResult> GetChatroomDetails(Guid chatroomId)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id");
                if(userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                    return Unauthorized(new { message = "User ID claim is missing." });

                if(!Guid.TryParse(userIdClaim.Value, out var userId))
                    return BadRequest(new { message = "Invalid User ID format." });

                var chatroom = await _chatroomService.GetChatroomDetailsAsync(userId, chatroomId);
                return Ok(chatroom);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chatroom details");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// Tạo chatroom direct (1-1)
    
        [HttpPost("direct")]
        public async Task<IActionResult> CreateDirectChatroom([FromBody] CreateDirectChatRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id");
                if(userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                    return Unauthorized(new { message = "User ID claim is missing." });

                if(!Guid.TryParse(userIdClaim.Value, out var userId))
                    return BadRequest(new { message = "Invalid User ID format." });

                var chatroom = await _chatroomService.CreateDirectChatroomAsync(userId, request.OtherUserId);
                return Ok(chatroom);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating direct chatroom");
                return BadRequest(new { message = ex.Message });
            }
        }

        
        /// Tạo group chatroom
        
        [HttpPost("group")]
        public async Task<IActionResult> CreateGroupChatroom([FromBody] CreateGroupChatroomRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id");
                if(userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                    return Unauthorized(new { message = "User ID claim is missing." });

                if(!Guid.TryParse(userIdClaim.Value, out var userId))
                    return BadRequest(new { message = "Invalid User ID format." });

                var chatroom = await _chatroomService.CreateGroupChatroomAsync(userId, request);
                return Ok(chatroom);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group chatroom");
                return BadRequest(new { message = ex.Message });
            }
        }

        
        /// Cập nhật thông tin group
        

        [HttpPut("{chatroomId}")]
        public async Task<IActionResult> UpdateGroupInfo(Guid chatroomId, [FromBody] UpdateChatroomRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id");
                if(userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                    return Unauthorized(new { message = "User ID claim is missing." });

                if(!Guid.TryParse(userIdClaim.Value, out var userId))
                    return BadRequest(new { message = "Invalid User ID format." });

                var chatroom = await _chatroomService.UpdateChatroomInfoAsync(userId, chatroomId, request);
                return Ok(chatroom);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group info");
                return BadRequest(new { message = ex.Message });
            }
        }

        
        /// Thêm members vào group
        
        [HttpPost("{chatroomId}/members")]
        public async Task<IActionResult> AddMembers(Guid chatroomId, [FromBody] AddMembersRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id");
                if(userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                    return Unauthorized(new { message = "User ID claim is missing." });

                if(!Guid.TryParse(userIdClaim.Value, out var userId))
                    return BadRequest(new { message = "Invalid User ID format." });

                var result = await _chatroomService.AddMembersAsync(userId, chatroomId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding members");
                return BadRequest(new { message = ex.Message });
            }
        }

        
        /// Xóa member khỏi group
       
        [HttpDelete("{chatroomId}/members/{memberId}")]
        public async Task<IActionResult> RemoveMember(Guid chatroomId, Guid memberId)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id");
                if(userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                    return Unauthorized(new { message = "User ID claim is missing." });

                if(!Guid.TryParse(userIdClaim.Value, out var userId))
                    return BadRequest(new { message = "Invalid User ID format." });

                var result = await _chatroomService.RemoveMemberAsync(userId, chatroomId, memberId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing member");
                return BadRequest(new { message = ex.Message });
            }
        }

        
        /// Cập nhật role member
        
        [HttpPut("{chatroomId}/members/{memberId}/role")]
        public async Task<IActionResult> UpdateMemberRole(Guid chatroomId, Guid memberId, [FromBody] UpdateMemberPermissionsRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id");
                if(userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                    return Unauthorized(new { message = "User ID claim is missing." });

                if(!Guid.TryParse(userIdClaim.Value, out var userId))
                    return BadRequest(new { message = "Invalid User ID format." });

                var result = await _chatroomService.UpdateMemberPermissionAsync(userId, chatroomId, memberId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating member role");
                return BadRequest(new { message = ex.Message });
            }
        }

        
        /// Rời khỏi group
       
        [HttpPost("{chatroomId}/leave")]
        public async Task<IActionResult> LeaveChatroom(Guid chatroomId)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id");
                if(userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                    return Unauthorized(new { message = "User ID claim is missing." });

                if(!Guid.TryParse(userIdClaim.Value, out var userId))
                    return BadRequest(new { message = "Invalid User ID format." });

                var result = await _chatroomService.LeaveChatroomAsync(userId, chatroomId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving chatroom");
                return BadRequest(new { message = ex.Message });
            }
        }

        
        /// Archive/Unarchive chatroom
       
        [HttpPut("{chatroomId}/archive")]
        public async Task<IActionResult> ArchiveChatroom(Guid chatroomId, [FromBody] ArchiveChatroomRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id");
                if(userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                    return Unauthorized(new { message = "User ID claim is missing." });

                if(!Guid.TryParse(userIdClaim.Value, out var userId))
                    return BadRequest(new { message = "Invalid User ID format." });

                var result = await _chatroomService.ArchiveChatroomAsync(userId, chatroomId, request.IsArchived);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving chatroom");
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion

       
        #region Group Invitations

       
        /// Gửi lời mời vào group
        
        [HttpPost("chatrooms/{chatroomId}/invitations")]
        public async Task<IActionResult> SendGroupInvitation(Guid chatroomId, [FromBody] SendGroupInvitationRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id");
                if(userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                    return Unauthorized(new { message = "User ID claim is missing." });

                if(!Guid.TryParse(userIdClaim.Value, out var userId))
                    return BadRequest(new { message = "Invalid User ID format." });

                var result = await _chatroomService.SendGroupInvitationAsync(userId, chatroomId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending group invitation");
                return BadRequest(new { message = ex.Message });
            }
        }

      
        /// Lấy danh sách invitations đã nhận
        
        [HttpGet("invitations/received")]
        public async Task<IActionResult> GetReceivedInvitations()
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id");
                if(userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                    return Unauthorized(new { message = "User ID claim is missing." });

                if(!Guid.TryParse(userIdClaim.Value, out var userId))
                    return BadRequest(new { message = "Invalid User ID format." });

                var invitations = await _chatroomService.GetReceivedInvitationsAsync(userId);
                return Ok(invitations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting received invitations");
                return BadRequest(new { message = ex.Message });
            }
        }

      
        /// Chấp nhận lời mời vào group
        
        [HttpPost("invitations/{invitationId}/accept")]
        public async Task<IActionResult> AcceptGroupInvitation(Guid invitationId)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id");
                if(userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                    return Unauthorized(new { message = "User ID claim is missing." });

                if(!Guid.TryParse(userIdClaim.Value, out var userId))
                    return BadRequest(new { message = "Invalid User ID format." });

                var result = await _chatroomService.AcceptGroupInvitationAsync(userId, invitationId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting group invitation");
                return BadRequest(new { message = ex.Message });
            }
        }

      
        /// Từ chối lời mời vào group
        
        [HttpPost("invitations/{invitationId}/reject")]
        public async Task<IActionResult> RejectGroupInvitation(Guid invitationId)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id");
                if(userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                    return Unauthorized(new { message = "User ID claim is missing." });

                if(!Guid.TryParse(userIdClaim.Value, out var userId))
                    return BadRequest(new { message = "Invalid User ID format." });

                var result = await _chatroomService.RejectGroupInvitationAsync(userId, invitationId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting group invitation");
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion
    }

}