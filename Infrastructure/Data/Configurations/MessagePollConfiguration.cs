using linksy_backend_api.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class MessagePollConfiguration : IEntityTypeConfiguration<MessagePoll>
    {
        public void Configure(EntityTypeBuilder<MessagePoll> entity)
        {
            entity.HasKey(e => e.MessageId).HasName("message_polls_pkey");
            entity.ToTable("message_polls");

            entity.Property(e => e.MessageId)
                .ValueGeneratedNever()
                .HasColumnName("message_id");
            entity.Property(e => e.Question)
                .HasMaxLength(200)
                .HasColumnName("question");
            entity.Property(e => e.IsClosed)
                .HasDefaultValue(false)
                .HasColumnName("is_closed");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ClosedAt)
                .HasColumnName("closed_at");
            entity.Property(e => e.CreatedByUserId)
                .HasColumnName("created_by_user_id");

            entity.HasOne(d => d.Message)
                .WithOne(p => p.Poll)
                .HasForeignKey<MessagePoll>(d => d.MessageId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("message_polls_message_id_fkey");

            entity.HasOne(d => d.CreatedByUser)
                .WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("message_polls_created_by_user_id_fkey");
        }
    }
}
