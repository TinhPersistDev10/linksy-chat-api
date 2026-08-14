using linksy_backend_api.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations;

public class UserReportConfiguration : IEntityTypeConfiguration<UserReport>
{
    public void Configure(EntityTypeBuilder<UserReport> entity)
    {
        entity.HasKey(e => e.ReportId).HasName("user_reports_pkey");

        entity.ToTable("user_reports", tb =>
            tb.HasComment("CHECK (reporter_user_id != reported_user_id)"));

        entity.HasIndex(e => new { e.ReportedUserId, e.Status }, "user_reports_reported_user_id_status_idx");
        entity.HasIndex(e => new { e.ReporterUserId, e.CreatedAt }, "user_reports_reporter_user_id_created_at_idx");
        entity.HasIndex(e => new { e.Status, e.CreatedAt }, "user_reports_status_created_at_idx");
        entity.HasIndex(e => new { e.ReporterUserId, e.ReportedUserId, e.Status },
                "user_reports_reporter_reported_pending_idx")
            .IsUnique()
            .HasFilter("status = 'pending'");

        entity.Property(e => e.ReportId)
            .ValueGeneratedNever()
            .HasColumnName("report_id");
        entity.Property(e => e.ReporterUserId).HasColumnName("reporter_user_id");
        entity.Property(e => e.ReportedUserId).HasColumnName("reported_user_id");
        entity.Property(e => e.Reason)
            .HasMaxLength(50)
            .HasColumnName("reason");
        entity.Property(e => e.Description)
            .HasMaxLength(2000)
            .HasColumnName("description");
        entity.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValue("pending")
            .HasColumnName("status");
        entity.Property(e => e.AdminNote)
            .HasMaxLength(2000)
            .HasColumnName("admin_note");
        entity.Property(e => e.ReviewedByAdminId).HasColumnName("reviewed_by_admin_id");
        entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        entity.HasOne(d => d.ReporterUser)
            .WithMany(p => p.ReportsSubmitted)
            .HasForeignKey(d => d.ReporterUserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("user_reports_reporter_user_id_fkey");

        entity.HasOne(d => d.ReportedUser)
            .WithMany(p => p.ReportsReceived)
            .HasForeignKey(d => d.ReportedUserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("user_reports_reported_user_id_fkey");

        entity.HasOne(d => d.ReviewedByAdmin)
            .WithMany()
            .HasForeignKey(d => d.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("user_reports_reviewed_by_admin_id_fkey");
    }
}
