using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Core.Interfaces.Repositories;
using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Domain.Interfaces.Repositories;
using linksy_backend_api.Models;

namespace linksy_backend_api.Repositories.IRepositories
{
    public interface IUnitOfWork : IDisposable
    {

        #region User management repositories
        IRepository<User> Users { get; }
        IRepository<Role> Roles { get; }
        IRepository<UserRole> UserRoles { get; }
        IRepository<AccessToken> AccessTokens { get; }
        IRepository<EmailOtp> EmailOtps { get; }

        #endregion

        #region Chatroom repositories
        IRepository<Chatroom> Chatrooms { get; }
        IRepository<ChatroomMember> ChatroomMembers { get; }
        IRepository<GroupInvitation> GroupInvitations { get; }
        IRepository<MemberPermission> MemberPermissions { get; }

        #endregion

        #region Message repositories
        IRepository<Message> Messages { get; }
        #endregion
        #region Social Repositories
        IRepository<Friendship> Friendships { get; }
        IRepository<FriendRequest> FriendRequests { get; }
        IRepository<BlockedUser> BlockedUsers { get; }
        #endregion

        #region Notification repositories
        IRepository<Notification> Notifications { get; }
        #endregion

        #region Specialized Repositories
        IChatroomRepository ChatroomRepository { get; }
        IUserRepository UserRepository { get; }
        IMessageRepository MessageRepository { get; }
        IMemberPermissionRepository MemberPermissionRepository { get; }
        IFriendRequestRepository FriendRequestRepository { get; }
        IFriendshipRepository FriendshipRepository { get; }
        IBlockedUserRepository BlockedUserRepository { get; }
        INotificationRepository NotificationRepository { get; }
        #endregion

        #region  Transaction methods
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        int SaveChanges();
        #endregion
    }

}