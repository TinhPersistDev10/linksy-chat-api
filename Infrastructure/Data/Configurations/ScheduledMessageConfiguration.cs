using linksy_backend_api.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations;

public class ScheduledMessageConfiguration : IEntityTypeConfiguration<ScheduledMessage>
{
    public void Configure(EntityTypeBuilder<ScheduledMessage> entity)
    {
        entity.HasKey(e => e.Id).HasName("scheduled_messages_pkey");
        entity.ToTable("scheduled_messages");

        entity.HasIndex(e => new { e.Status, e.SendAt })
            .HasDatabaseName("scheduled_messages_due_idx");

        entity.HasIndex(e => new { e.ChatroomId, e.SenderId, e.Status, e.SendAt })
            .HasDatabaseName("scheduled_messages_room_sender_status_idx");

        entity.Property(e => e.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");
        entity.Property(e => e.ChatroomId).HasColumnName("chatroom_id");
        entity.Property(e => e.SenderId).HasColumnName("sender_id");
        entity.Property(e => e.MessageType)
            .HasMaxLength(20)
            .HasColumnName("message_type");
        entity.Property(e => e.MessageText).HasColumnName("message_text");
        entity.Property(e => e.ParentMessageId).HasColumnName("parent_message_id");
        entity.Property(e => e.SendAt).HasColumnName("send_at");
        entity.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValue("pending")
            .HasColumnName("status");
        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");

        entity.HasOne(d => d.Chatroom)
            .WithMany()
            .HasForeignKey(d => d.ChatroomId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("scheduled_messages_chatroom_id_fkey");

        entity.HasOne(d => d.Sender)
            .WithMany()
            .HasForeignKey(d => d.SenderId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("scheduled_messages_sender_id_fkey");

        entity.HasOne(d => d.ParentMessage)
            .WithMany()
            .HasForeignKey(d => d.ParentMessageId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("scheduled_messages_parent_message_id_fkey");
    }
}
