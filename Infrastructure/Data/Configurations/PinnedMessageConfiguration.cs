using linksy_backend_api.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class PinnedMessageConfiguration : IEntityTypeConfiguration<PinnedMessage>
    {
        public void Configure(EntityTypeBuilder<PinnedMessage> entity)
        {
            entity.HasKey(e => e.PinnedMessageId).HasName("pinned_messages_pkey");

            entity.ToTable("pinned_messages");

            entity.HasIndex(e => new { e.ChatroomId, e.MessageId }, "pinned_messages_chatroom_id_message_id_key")
                .IsUnique();
            entity.HasIndex(e => new { e.ChatroomId, e.PinnedAt }, "pinned_messages_chatroom_id_pinned_at_idx");
            entity.HasIndex(e => e.MessageId, "pinned_messages_message_id_idx");

            entity.Property(e => e.PinnedMessageId)
                .ValueGeneratedNever()
                .HasColumnName("pinned_message_id");
            entity.Property(e => e.ChatroomId)
                .HasColumnName("chatroom_id");
            entity.Property(e => e.MessageId)
                .HasColumnName("message_id");
            entity.Property(e => e.PinnedByUserId)
                .HasColumnName("pinned_by_user_id");
            entity.Property(e => e.PinnedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("pinned_at");

            entity.HasOne(d => d.Chatroom)
                .WithMany(p => p.PinnedMessages)
                .HasForeignKey(d => d.ChatroomId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("pinned_messages_chatroom_id_fkey");

            entity.HasOne(d => d.Message)
                .WithMany(p => p.PinnedMessages)
                .HasForeignKey(d => d.MessageId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("pinned_messages_message_id_fkey");

            entity.HasOne(d => d.PinnedByUser)
                .WithMany(p => p.PinnedMessages)
                .HasForeignKey(d => d.PinnedByUserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("pinned_messages_pinned_by_user_id_fkey");
        }
    }
}
