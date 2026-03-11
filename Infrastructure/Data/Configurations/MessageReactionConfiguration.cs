using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
    {
        public void Configure(EntityTypeBuilder<MessageReaction> entity)
        {
            entity.HasKey(e => e.ReactionId).HasName("message_reactions_pkey");

            entity.ToTable("message_reactions");

            entity.HasIndex(e => new { e.MessageId, e.UserId, e.EmojiCode }, "message_reactions_message_id_user_id_emoji_code_key").IsUnique();
            entity.HasIndex(e => new { e.MessageId, e.EmojiCode }, "message_reactions_message_id_emoji_code_idx");
            entity.HasIndex(e => new { e.UserId, e.ReactedAt }, "message_reactions_user_id_reacted_at_idx");

            entity.Property(e => e.ReactionId)
                .ValueGeneratedNever()
                .HasColumnName("reaction_id");
            entity.Property(e => e.MessageId)
                .HasColumnName("message_id");
            entity.Property(e => e.UserId)
                .HasColumnName("user_id");
            entity.Property(e => e.EmojiCode)
                .HasColumnType("character varying")
                .HasColumnName("emoji_code");
            entity.Property(e => e.ReactedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("reacted_at");

            entity.HasOne(d => d.Message)
                .WithMany(p => p.MessageReactions)
                .HasForeignKey(d => d.MessageId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("message_reactions_message_id_fkey");

            entity.HasOne(d => d.User)
                .WithMany(p => p.MessageReactions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("message_reactions_user_id_fkey");
        }
    }
}