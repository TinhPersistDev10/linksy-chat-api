using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.DTOs.Responses.Users;
using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.ChatroomDTO;
using linksy_backend_api.DTOs.MessagesDTOs;
using linksy_backend_api.Models;

namespace linksy_backend_api.Services.IServices
{
    public interface IChatroomService
    {
        #region Create chatrooms
        Task<ChatroomResponseDto> CreateDirectChatroomAsync(Guid userId, Guid otherUserId);
        Task<ChatroomResponseDto> CreateGroupChatroomAsync(Guid userId, CreateGroupChatroomRequest groupDto);
        #endregion

        #region GET chatrooms and details

        Task<List<ChatroomResponseDto>> GetUserChatroomsAsync(Guid userId, bool includeArchived = false, string? roomType = null);
        Task<ChatroomResponseDto> GetChatroomDetailsAsync(Guid userId, Guid chatroomId);
        #endregion

        #region Update chatrooms and members
        Task<ChatroomResponseDto> UpdateChatroomInfoAsync(Guid userId, Guid chatroomId, UpdateChatroomRequest request);
        Task<AvatarResponse> UpdateGroupAvatarAsync(Guid userId, Guid chatroomId, IFormFile avatarFile);
        Task<ApiResponseDto> UpdateMemberPermissionsAsync(Guid userId, Guid chatroomId, Guid memberId, UpdateMemberPermissionsRequest request);
        Task<ApiResponseDto> UpdateMemberNicknameAsync(Guid userId, Guid chatroomId, Guid memberId, string? nickname);
        Task<ApiResponseDto> UpdateChatroomQuickEmojiAsync(Guid userId, Guid chatroomId, string emoji);
        #endregion

        #region Add/Remove members
        Task<ApiResponseDto> AddMembersAsync(Guid userId, Guid chatroomId, AddMembersRequest request);
        Task<ApiResponseDto> RemoveMemberAsync(Guid userId, Guid chatroomId, Guid memberId);
        Task<ApiResponseDto> LeaveChatroomAsync(Guid userId, Guid chatroomId);
        Task<ApiResponseDto> PromoteToAdminAsync(Guid userId, Guid chatroomId, Guid memberId);
        Task<ApiResponseDto> DemoteFromAdminAsync(Guid userId, Guid chatroomId, Guid memberId);
        Task<ApiResponseDto> DisbandChatroomAsync(Guid userId, Guid chatroomId);
        #endregion

        
        Task<ApiResponseDto> ArchiveChatroomAsync(Guid userId, Guid chatroomId, bool isArchived);
        Task<ApiResponseDto> PinChatroomAsync(Guid userId, Guid chatroomId, bool isPinned);
        Task<ApiResponseDto> MuteChatroomAsync(Guid userId, Guid chatroomId, bool isMuted, DateTime? muteUntil = null);
        Task<ApiResponseDto> ClearConversationAsync(Guid userId, Guid chatroomId);
        Task<AvatarResponse> DeleteGroupAvatarAsync(Guid userId, Guid chatroomId);
    }
}