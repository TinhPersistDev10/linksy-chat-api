using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class ChatroomMemberConfiguration : IEntityTypeConfiguration<ChatroomMember>
    {
        public void Configure(EntityTypeBuilder<ChatroomMember> entity)
        {
            entity.HasKey(e => e.MemberId).HasName("chatroom_members_pkey");

            entity.ToTable("chatroom_members");

            entity.HasIndex(e => new { e.ChatroomId, e.MemberRole }, "chatroom_members_chatroom_id_member_role_idx");
            entity.HasIndex(e => new { e.ChatroomId, e.UserId }, "chatroom_members_chatroom_id_user_id_idx").IsUnique();
            entity.HasIndex(e => new { e.UserId, e.ChatroomId }, "chatroom_members_user_id_chatroom_id_idx");
            entity.HasIndex(e => new { e.UserId, e.LeftAt }, "chatroom_members_user_id_left_at_idx");
            entity.HasIndex(e => new { e.ChatroomId, e.LastReadAt }, "chatroom_members_chatroom_id_last_read_at_idx");

            //member_id
            entity.Property(e => e.MemberId)
                .ValueGeneratedNever()
                .HasColumnName("member_id");

            //chatroom_id
            entity.Property(e => e.ChatroomId).HasColumnName("chatroom_id");

            //user_id
            entity.Property(e => e.UserId)
                .HasColumnName("user_id");

            //member_role
            entity.Property(e => e.MemberRole)
                .HasDefaultValueSql("'member'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("member_role");

            //joined_at
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("joined_at");
            //left_at
            entity.Property(e => e.LeftAt)
                .HasColumnName("left_at");


            //nickname
            entity.Property(e => e.Nickname)
                .HasMaxLength(100)
                .HasColumnType("character varying")
                .HasColumnName("nickname");

            //isMuted
            entity.Property(e => e.IsMuted)
                .HasDefaultValue(false)
                .HasColumnName("is_muted");
            //muted_until
            entity.Property(e => e.MutedUntil)
                .HasColumnName("muted_until");
            //notification preference
            entity.Property(e => e.NotificationPreference)
                .HasMaxLength(50)
                .HasDefaultValueSql("'all::character varying")
                .HasColumnType("character varying")
                .HasColumnName("notification_preference");
            //last_read_at
            entity.Property(e => e.LastReadAt)
                .HasColumnName("last_read_at");

            //message_count
            entity.Property(e => e.MessageCount)
                .HasDefaultValue(0)
                .HasColumnName("message_count");

            // is_pinned / pinned_at (per-user conversation pin)
            entity.Property(e => e.IsPinned)
                .HasDefaultValue(false)
                .HasColumnName("is_pinned");
            entity.Property(e => e.PinnedAt)
                .HasColumnName("pinned_at");

            // cleared_at — hide history for this member (delete conversation)
            entity.Property(e => e.ClearedAt)
                .HasColumnName("cleared_at");

            entity.HasIndex(e => new { e.UserId, e.IsPinned, e.PinnedAt }, "chatroom_members_user_id_is_pinned_pinned_at_idx");

            //added_by
            entity.Property(e => e.AddedBy)
                .HasColumnName("added_by");

            //removed_by
            entity.Property(e => e.RemovedBy)
                .HasColumnName("removed_by");


            //Foreign key

            entity.HasOne(d => d.Chatroom)
               .WithMany(p => p.ChatroomMembers)
               .HasForeignKey(d => d.ChatroomId)
               .OnDelete(DeleteBehavior.Cascade)
               .HasConstraintName("chatroom_members_chatroom_id_fkey");

            entity.HasOne(d => d.User)
                .WithMany(p => p.ChatroomMembers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("chatroom_members_user_id_fkey");

            entity.HasOne(d => d.AddedByNavigation)
                .WithMany()
                .HasForeignKey(d => d.AddedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("chatroom_members_added_by_fkey");

            entity.HasOne(d => d.RemovedByNavigation)
                .WithMany()
                .HasForeignKey(d => d.RemovedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("chatroom_members_removed_by_fkey");
        }


    }
}