using linksy_backend_api.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class MessageMentionConfiguration : IEntityTypeConfiguration<MessageMention>
    {
        public void Configure(EntityTypeBuilder<MessageMention> entity)
        {
            entity.HasKey(e => new { e.MessageId, e.MentionedUserId })
                .HasName("message_mentions_pkey");

            entity.ToTable("message_mentions");

            entity.HasIndex(e => e.MentionedUserId, "message_mentions_mentioned_user_id_idx");
            entity.HasIndex(e => e.MessageId, "message_mentions_message_id_idx");

            entity.Property(e => e.MessageId)
                .HasColumnName("message_id");
            entity.Property(e => e.MentionedUserId)
                .HasColumnName("mentioned_user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Message)
                .WithMany(p => p.MessageMentions)
                .HasForeignKey(d => d.MessageId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("message_mentions_message_id_fkey");

            entity.HasOne(d => d.MentionedUser)
                .WithMany(p => p.MessageMentions)
                .HasForeignKey(d => d.MentionedUserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("message_mentions_mentioned_user_id_fkey");
        }
    }
}
