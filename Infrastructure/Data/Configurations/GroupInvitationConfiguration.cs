using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class GroupInvitationConfiguration : IEntityTypeConfiguration<GroupInvitation>
    {
        public void Configure(EntityTypeBuilder<GroupInvitation> entity)
        {
            entity.HasKey(e => e.InvitationId).HasName("group_invitations_pkey");

            entity.ToTable("group_invitations");

            entity.HasIndex(e => new { e.ChatroomId, e.InvitedUserId, e.Status }, "group_invitations_chatroom_id_invited_user_id_status_idx");
            entity.HasIndex(e => new { e.ChatroomId, e.Status }, "group_invitations_chatroom_id_status_idx");
            entity.HasIndex(e => e.ExpiresAt, "group_invitations_expires_at_idx");
            entity.HasIndex(e => new { e.InvitedUserId, e.Status, e.SentAt }, "group_invitations_invited_user_id_status_sent_at_idx");

            entity.Property(e => e.InvitationId)
                .ValueGeneratedNever()
                .HasColumnName("invitation_id");
            entity.Property(e => e.ChatroomId).HasColumnName("chatroom_id");
            entity.Property(e => e.ExpiresAt)
                .HasColumnName("expires_at");
            entity.Property(e => e.InvitedBy).HasColumnName("invited_by");
            entity.Property(e => e.InvitedUserId).HasColumnName("invited_user_id");
            entity.Property(e => e.Message)
                .HasColumnType("character varying")
                .HasColumnName("message");
            entity.Property(e => e.RespondedAt)
                .HasColumnName("responded_at");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("sent_at");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.Chatroom).WithMany(p => p.GroupInvitations)
                .HasForeignKey(d => d.ChatroomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("group_invitations_chatroom_id_fkey");

            entity.HasOne(d => d.InvitedByNavigation).WithMany(p => p.GroupInvitationInvitedByNavigations)
                .HasForeignKey(d => d.InvitedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("group_invitations_invited_by_fkey");

            entity.HasOne(d => d.InvitedUser).WithMany(p => p.GroupInvitationInvitedUsers)
                .HasForeignKey(d => d.InvitedUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("group_invitations_invited_user_id_fkey");
        }

    }
}   