using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> entity)
        {
            entity.HasKey(e => e.MessageId).HasName("messages_pkey");

            entity.ToTable("messages");

            entity.HasIndex(e => new { e.ChatroomId, e.IsDeleted, e.SentAt }, "messages_chatroom_id_is_deleted_sent_at_idx");
            entity.HasIndex(e => new { e.ChatroomId, e.SentAt }, "messages_chatroom_id_sent_at_idx");
            entity.HasIndex(e => new { e.MessageType, e.ChatroomId }, "messages_message_type_chatroom_id_idx");
            entity.HasIndex(e => new { e.ParentMessageId, e.SentAt }, "messages_parent_message_id_sent_at_idx");
            entity.HasIndex(e => new { e.SenderId, e.SentAt }, "messages_sender_id_sent_at_idx");

            entity.Property(e => e.MessageId)
                .ValueGeneratedNever()
                .HasColumnName("message_id");
            entity.Property(e => e.ChatroomId).HasColumnName("chatroom_id");
            entity.Property(e => e.DeletedAt)
                .HasColumnName("deleted_at");
            entity.Property(e => e.EditedAt)
                .HasColumnName("edited_at");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.IsEdited)
                .HasDefaultValue(false)
                .HasColumnName("is_edited");
            entity.Property(e => e.MessageText).HasColumnName("message_text");
            entity.Property(e => e.MessageType)
                .HasColumnType("character varying")
                .HasColumnName("message_type");
            entity.Property(e => e.ParentMessageId).HasColumnName("parent_message_id");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("sent_at");

            entity.HasOne(d => d.Chatroom).WithMany(p => p.Messages)
                .HasForeignKey(d => d.ChatroomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("messages_chatroom_id_fkey");

            entity.HasOne(d => d.ParentMessage).WithMany(p => p.InverseParentMessage)
                .HasForeignKey(d => d.ParentMessageId)
                .HasConstraintName("messages_parent_message_id_fkey");

            entity.HasOne(d => d.Sender).WithMany(p => p.Messages)
                .HasForeignKey(d => d.SenderId)
                .HasConstraintName("messages_sender_id_fkey");
        }


    }
}