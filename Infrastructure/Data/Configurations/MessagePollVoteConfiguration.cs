using linksy_backend_api.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class MessagePollVoteConfiguration : IEntityTypeConfiguration<MessagePollVote>
    {
        public void Configure(EntityTypeBuilder<MessagePollVote> entity)
        {
            entity.HasKey(e => e.VoteId).HasName("message_poll_votes_pkey");
            entity.ToTable("message_poll_votes");

            entity.HasIndex(e => new { e.MessageId, e.UserId }, "message_poll_votes_message_id_user_id_key")
                .IsUnique();
            entity.HasIndex(e => e.OptionId, "message_poll_votes_option_id_idx");

            entity.Property(e => e.VoteId)
                .ValueGeneratedNever()
                .HasColumnName("vote_id");
            entity.Property(e => e.OptionId)
                .HasColumnName("option_id");
            entity.Property(e => e.MessageId)
                .HasColumnName("message_id");
            entity.Property(e => e.UserId)
                .HasColumnName("user_id");
            entity.Property(e => e.VotedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("voted_at");

            entity.HasOne(d => d.Option)
                .WithMany(p => p.Votes)
                .HasForeignKey(d => d.OptionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("message_poll_votes_option_id_fkey");

            entity.HasOne(d => d.Poll)
                .WithMany()
                .HasForeignKey(d => d.MessageId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("message_poll_votes_message_id_fkey");

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("message_poll_votes_user_id_fkey");
        }
    }
}
