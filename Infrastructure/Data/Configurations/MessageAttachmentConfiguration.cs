using linksy_backend_api.Domain.Entities.Models;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class MessageAttachmentConfiguration : IEntityTypeConfiguration<MessageAttachment>
    {
        public void Configure(EntityTypeBuilder<MessageAttachment> entity)
        {
            entity.HasKey(e => e.AttachmentId).HasName("message_attachments_pkey");

            entity.ToTable("message_attachments");

            entity.HasIndex(e => e.MessageId, "message_attachments_message_id_idx");
            entity.HasIndex(e => new { e.MessageId, e.AttachmentType }, "message_attachments_message_id_attachment_type_idx");

            entity.Property(e => e.AttachmentId)
                .ValueGeneratedNever()
                .HasColumnName("attachment_id");
            entity.Property(e => e.MessageId)
                .HasColumnName("message_id");
            entity.Property(e => e.AttachmentType)
                .HasColumnType("character varying")
                .HasColumnName("attachment_type");
            entity.Property(e => e.FileName)
                .HasColumnType("character varying")
                .HasColumnName("file_name");
            entity.Property(e => e.FilePath)
                .HasColumnType("text")
                .HasColumnName("file_path");
            entity.Property(e => e.FileSize)
                .HasColumnName("file_size");
            entity.Property(e => e.MimeType)
                .HasColumnType("character varying")
                .HasColumnName("mime_type");
            entity.Property(e => e.CdnUrl)
                .HasColumnType("text")
                .HasColumnName("cdn_url");
            entity.Property(e => e.ThumbnailUrl)
                .HasColumnType("text")
                .HasColumnName("thumbnail_url");
            entity.Property(e => e.Width)
                .HasColumnName("width");
            entity.Property(e => e.Height)
                .HasColumnName("height");
            entity.Property(e => e.DurationMs)
                .HasColumnName("duration_ms");
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("uploaded_at");

            entity.HasOne(d => d.Message)
                .WithMany(p => p.MessageAttachments)
                .HasForeignKey(d => d.MessageId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("message_attachments_message_id_fkey");
        }
    }
}