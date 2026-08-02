using linksy_backend_api.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class MessagePollOptionConfiguration : IEntityTypeConfiguration<MessagePollOption>
    {
        public void Configure(EntityTypeBuilder<MessagePollOption> entity)
        {
            entity.HasKey(e => e.OptionId).HasName("message_poll_options_pkey");
            entity.ToTable("message_poll_options");

            entity.HasIndex(e => e.MessageId, "message_poll_options_message_id_idx");
            entity.HasIndex(e => new { e.MessageId, e.SortOrder }, "message_poll_options_message_id_sort_order_idx");

            entity.Property(e => e.OptionId)
                .ValueGeneratedNever()
                .HasColumnName("option_id");
            entity.Property(e => e.MessageId)
                .HasColumnName("message_id");
            entity.Property(e => e.Text)
                .HasMaxLength(100)
                .HasColumnName("text");
            entity.Property(e => e.SortOrder)
                .HasColumnName("sort_order");

            entity.HasOne(d => d.Poll)
                .WithMany(p => p.Options)
                .HasForeignKey(d => d.MessageId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("message_poll_options_message_id_fkey");
        }
    }
}
