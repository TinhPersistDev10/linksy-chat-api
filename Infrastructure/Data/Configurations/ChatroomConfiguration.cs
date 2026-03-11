using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class ChatroomConfiguration : IEntityTypeConfiguration<Chatroom>
    {
        public void Configure(EntityTypeBuilder<Chatroom> entity)
        {
            entity.HasKey(e => e.ChatroomId).HasName("chatrooms_pkey");

            entity.ToTable("chatrooms");

            entity.HasIndex(e => new { e.CreatedBy, e.RoomType }, "chatrooms_created_by_room_type_idx");
            entity.HasIndex(e => new { e.IsArchived, e.LastActivityAt }, "chatrooms_is_archived_last_activity_at_idx");
            entity.HasIndex(e => e.LastActivityAt, "chatrooms_last_activity_at_idx");
            entity.HasIndex(e => new { e.RoomType, e.IsActive, e.LastActivityAt }, "chatrooms_room_type_is_active_last_activity_at_idx");

            entity.Property(e => e.ChatroomId)
                .ValueGeneratedNever()
                .HasColumnName("chatroom_id");
            entity.Property(e => e.Avatar)
                .HasColumnType("character varying")
                .HasColumnName("avatar");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description)
                .HasColumnType("character varying")
                .HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsArchived)
                .HasDefaultValue(false)
                .HasColumnName("is_archived");
            entity.Property(e => e.LastActivityAt)
                .HasColumnName("last_activity_at");
            entity.Property(e => e.LastMessageAt)
                .HasColumnName("last_message_at");
            entity.Property(e => e.LastMessageId).HasColumnName("last_message_id");
            entity.Property(e => e.RoomName)
                .HasColumnType("character varying")
                .HasColumnName("room_name");
            entity.Property(e => e.RoomType)
                .HasColumnType("character varying")
                .HasColumnName("room_type");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Chatrooms)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("chatrooms_created_by_fkey");

            entity.HasOne(d => d.LastMessage).WithMany(p => p.Chatrooms)
                .HasForeignKey(d => d.LastMessageId)
                .HasConstraintName("chatrooms_last_message_id_fkey");
        }
    }
}