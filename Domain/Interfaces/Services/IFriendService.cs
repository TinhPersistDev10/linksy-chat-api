using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.DTOs.Responses.Friends;
using linksy_backend_api.DTOs;
using linksy_backend_api.DTOs.Block;
using linksy_backend_api.DTOs.FriendDTO;
using linksy_backend_api.DTOs.RelationshipDTO;
using linksy_backend_api.DTOs.UserDTO;
using linksy_backend_api.Models;

namespace linksy_backend_api.Services.IServices
{
    public interface IFriendService
    {

        Task<List<FriendDto>> GetFriendsAsync(Guid userId);
        Task<List<UserSearchDto>> SearchUsersAsync(Guid userId, string query, int limit);
        // //Gui yeu cau ket bạn
        Task<FriendRequestResponse> SendFriendRequestAsync(Guid senderId, SendFriendRequest request);
        Task<List<FriendRequestResponse>> GetReceivedFriendRequestsAsync(Guid userId);
         Task<List<FriendRequestResponse>> GetSentFriendRequestsAsync(Guid userId);
        Task<ApiResponseDto> AcceptFriendRequestAsync(Guid userId, Guid requestId);
        Task<ApiResponseDto> RejectFriendRequestAsync(Guid userId, Guid requestId);
        Task<ApiResponseDto> CancelFriendRequestAsync(Guid userId, Guid requestId);
        Task<ApiResponseDto> UnfriendAsync(Guid userId, Guid friendId);
    }
}