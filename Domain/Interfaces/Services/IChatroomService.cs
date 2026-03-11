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

        Task<List<ChatroomResponseDto>> GetUserChatroomsAsync(Guid userId);
        Task<ChatroomResponseDto> GetChatroomDetailsAsync(Guid userId, Guid chatroomId);
        Task<List<GroupInvitationRequest>> GetReceivedInvitationsAsync(Guid userId);
        #endregion

        #region Update chatrooms and members
        Task<ChatroomResponseDto> UpdateChatroomInfoAsync(Guid userId, Guid chatroomId, UpdateChatroomRequest request);
        Task<ApiResponseDto> UpdateMemberPermissionAsync(Guid userId, Guid chatroomId, Guid memberId, UpdateMemberPermissionsRequest request);
        Task<AvatarResponse> UpdateGroupAvatarAsync(Guid userId, Guid chatroomId, IFormFile avatarFile);
        #endregion

        #region Add/Remove members
        Task<ApiResponseDto> AddMembersAsync(Guid userId, Guid chatroomId, AddMembersRequest request);
        Task<ApiResponseDto> RemoveMemberAsync(Guid userId, Guid chatroomId, Guid memberId);
        Task<ApiResponseDto> LeaveChatroomAsync(Guid userId, Guid chatroomId);
        #endregion

        
        Task<ChatroomResponseDto> MapToChatroomResponseAsync(Chatroom chatroom, Guid currentUserId);
        Task<ApiResponseDto> ArchiveChatroomAsync(Guid userId, Guid chatroomId, bool isArchived);
        Task<ApiResponseDto> SendGroupInvitationAsync(Guid userId, Guid chatroomId, SendGroupInvitationRequest request);
        Task<ApiResponseDto> AcceptGroupInvitationAsync(Guid userId, Guid invitationId);
        Task<ApiResponseDto> RejectGroupInvitationAsync(Guid userId, Guid invitationId);
        Task<AvatarResponse> DeleteGroupAvatarAsync(Guid userId, Guid chatroomId);
    }
}