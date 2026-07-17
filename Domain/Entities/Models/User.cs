using System;
using System.Collections.Generic;
using linksy_backend_api.Domain.Entities.Models;

namespace linksy_backend_api.Models;

public partial class User
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Fullname { get; set; }

    public string? Avatar { get; set; }

    public string? Bio { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsEmailVerified { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime? EmailVerifiedAt { get; set; }

    public int? FailedLoginAttempts { get; set; }

    public DateTime? AccountLockedUntil { get; set; }

    public DateTime? PasswordChangedAt { get; set; }

    public virtual ICollection<FriendRequest> SentFriendRequests { get; set; } = new List<FriendRequest>();
    public virtual ICollection<FriendRequest> ReceivedFriendRequests { get; set; } = new List<FriendRequest>();
    public virtual ICollection<AccessToken> AccessTokens { get; set; } = new List<AccessToken>();

    public virtual ICollection<BlockedUser> BlockedUserBlockedUserNavigations { get; set; } = new List<BlockedUser>();

    public virtual ICollection<BlockedUser> BlockedUserBlockerUsers { get; set; } = new List<BlockedUser>();

    public virtual ICollection<Chatroom> Chatrooms { get; set; } = new List<Chatroom>();

    public virtual ICollection<EmailOtp> EmailOtps { get; set; } = new List<EmailOtp>();

    public virtual ICollection<Friendship> FriendshipUser1s { get; set; } = new List<Friendship>();

    public virtual ICollection<Friendship> FriendshipUser2s { get; set; } = new List<Friendship>();

    public virtual ICollection<GroupInvitation> GroupInvitationInvitedByNavigations { get; set; } = new List<GroupInvitation>();

    public virtual ICollection<GroupInvitation> GroupInvitationInvitedUsers { get; set; } = new List<GroupInvitation>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<ChatroomMember> ChatroomMembers { get; set; } = new List<ChatroomMember>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    
    // In User.cs — ADD these navigation properties
    public virtual UserSettings? UserSettings { get; set; }
    public virtual NotificationSettings? NotificationSettings { get; set; }
    public virtual PrivacySettings? PrivacySettings { get; set; }
    public virtual UserStatus? UserStatus { get; set; }
    public virtual ICollection<MessageReaction> MessageReactions { get; set; } = new List<MessageReaction>();
    public virtual ICollection<MessageDelivery> MessageDeliveries { get; set; } = new List<MessageDelivery>();
    public virtual ICollection<MessageMention> MessageMentions { get; set; } = new List<MessageMention>();

}
