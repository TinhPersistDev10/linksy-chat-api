using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class MessageDeliveryConfiguration : IEntityTypeConfiguration<MessageDelivery>
    {
        public void Configure(EntityTypeBuilder<MessageDelivery> entity)
        {
            entity.HasKey(e => e.DeliveryId).HasName("message_deliveries_pkey");

            entity.ToTable("message_deliveries");

            entity.HasIndex(e => new { e.MessageId, e.UserId }, "message_deliveries_message_id_user_id_key").IsUnique();
            entity.HasIndex(e => new { e.MessageId, e.Status }, "message_deliveries_message_id_status_idx");
            entity.HasIndex(e => new { e.UserId, e.Status, e.ReadAt }, "message_deliveries_user_id_status_read_at_idx");

            entity.Property(e => e.DeliveryId)
                .ValueGeneratedNever()
                .HasColumnName("delivery_id");
            entity.Property(e => e.MessageId)
                .HasColumnName("message_id");
            entity.Property(e => e.UserId)
                .HasColumnName("user_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'sent'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("status");
            entity.Property(e => e.DeliveredAt)
                .HasColumnName("delivered_at");
            entity.Property(e => e.ReadAt)
                .HasColumnName("read_at");

            entity.HasOne(d => d.Message)
                .WithMany(p => p.MessageDeliveries)
                .HasForeignKey(d => d.MessageId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("message_deliveries_message_id_fkey");

            entity.HasOne(d => d.User)
                .WithMany(p => p.MessageDeliveries)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("message_deliveries_user_id_fkey");
        }
    }
}